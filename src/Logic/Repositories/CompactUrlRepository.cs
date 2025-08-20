using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Contracts;
using Logic.Hashing;

namespace Logic.Repositories;

/// <summary>
/// A compact, thread-safe repository that stores shortUrl -> small integer IDs in a hot concurrent map,
/// while interning original URL strings in a separate table to avoid duplication.
/// </summary>
/// <param name="concurrencyLevel">Estimated concurrent writers.</param>
/// <param name="capacity">Initial capacity hint.</param>
public class CompactUrlRepository(int concurrencyLevel, int capacity) : IUrlRepository, IDisposable
{
    // Hot path: short code -> interned ID
    private readonly ConcurrentDictionary<string, int> _shortToId = new(concurrencyLevel, capacity, StringComparer.Ordinal);

    // Interning maps
    private readonly ConcurrentDictionary<ulong, int> _hashToId = new(concurrencyLevel, capacity);
    private readonly ConcurrentDictionary<int, string> _idToUrl = new(concurrencyLevel, capacity);
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, int>> _hashCollisions = new(Environment.ProcessorCount, 31);

    private int _nextId; // starts at 0, Interlocked.Increment returns first id as 1

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactUrlRepository"/> class.
    /// </summary>
    public CompactUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)
    {
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetOriginalUrl(string shortUrl)
    {
        if (!_shortToId.TryGetValue(shortUrl, out var id))
        {
            return string.Empty;
        }

        return _idToUrl.TryGetValue(id, out var original) ? original : string.Empty;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
    int id = InternOriginal(originalUrl);
    return _shortToId.TryAdd(shortUrl, id);
    }

    /// <inheritdoc />
    public bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue)
    {
        // expectedValue == null => key should not exist
        if (expectedValue is null)
        {
            int idNew = InternOriginal(originalUrl);
            if (_shortToId.TryAdd(shortUrl, idNew))
            {
                currentValue = null;
                return true;
            }

            currentValue = GetCurrentOriginal(shortUrl);
            return false;
        }

        // Otherwise enforce CAS based on expected string value
        if (!_shortToId.TryGetValue(shortUrl, out var existingId))
        {
            currentValue = null;
            return false;
        }

        string? existingOriginal = _idToUrl.TryGetValue(existingId, out var s) ? s : null;
        if (!string.Equals(existingOriginal, expectedValue, StringComparison.Ordinal))
        {
            currentValue = existingOriginal;
            return false;
        }

        int newId = InternOriginal(originalUrl);
        if (_shortToId.TryUpdate(shortUrl, newId, existingId))
        {
            currentValue = null;
            return true;
        }

        currentValue = GetCurrentOriginal(shortUrl);
        return false;
    }

    /// <summary>
    /// Releases resources (clears dictionaries).
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by this instance and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _shortToId.Clear();
        _hashToId.Clear();
        _idToUrl.Clear();
        _hashCollisions.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetHash64(string value)
    {
        const int maxByteCount = Constants.MaxStackSize / 2;
        const ulong seed = 0xC0FFEEUL;

        // Encode as UTF-8 with minimal allocation; fall back to pooled buffer for larger strings.
        ReadOnlySpan<char> chars = value.AsSpan();
        int byteCount = Encoding.UTF8.GetByteCount(chars);

        if (byteCount <= maxByteCount)
        {
            Span<byte> buffer = stackalloc byte[byteCount];
            _ = Encoding.UTF8.GetBytes(chars, buffer);
            ReadOnlySpan<byte> ro = buffer;
            return Murmur3.Hash64(ref ro, seed);
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Span<byte> span = rented.AsSpan(0, byteCount);
            _ = Encoding.UTF8.GetBytes(chars, span);
            ReadOnlySpan<byte> ro = span;
            return Murmur3.Hash64(ref ro, seed);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    // Intern an original URL and return its stable integer ID.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int InternOriginal(string url)
    {
        ulong hash = GetHash64(url);

        // Fast path: hash -> id exists, verify content matches
        if (_hashToId.TryGetValue(hash, out var id))
        {
            if (_idToUrl.TryGetValue(id, out var existing) && string.Equals(existing, url, StringComparison.Ordinal))
            {
                return id;
            }

            // True 64-bit collision
            return InternCollision(hash, url);
        }

        // First claim on this hash
        int newId = Interlocked.Increment(ref _nextId);
        if (_hashToId.TryAdd(hash, newId))
        {
            _idToUrl.TryAdd(newId, url);
            return newId;
        }

        // Lost race; resolve against published entry
        if (_hashToId.TryGetValue(hash, out var racedId))
        {
            if (_idToUrl.TryGetValue(racedId, out var raced) && string.Equals(raced, url, StringComparison.Ordinal))
            {
                return racedId;
            }

            return InternCollision(hash, url);
        }

        return InternCollision(hash, url);
    }

    // Handle rare hash collisions via per-hash string -> id buckets.
    [ExcludeFromCodeCoverage]
    private int InternCollision(ulong hash, string url)
    {
        var bucket = _hashCollisions.GetOrAdd(hash, static _ => new ConcurrentDictionary<string, int>(StringComparer.Ordinal));

        if (bucket.TryGetValue(url, out var id))
        {
            return id;
        }

        int newId = Interlocked.Increment(ref _nextId);

        if (!_idToUrl.TryAdd(newId, url) && bucket.TryGetValue(url, out var existed))
        {
            return existed;
        }

        if (bucket.TryAdd(url, newId))
        {
            return newId;
        }

        _idToUrl.TryRemove(newId, out _);
        return bucket[url];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? GetCurrentOriginal(string shortUrl)
    {
        return _shortToId.TryGetValue(shortUrl, out int id) && _idToUrl.TryGetValue(id, out string? s) ? s : null;
    }
}
