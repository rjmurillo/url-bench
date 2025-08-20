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
    // Sharded state holder, initialized using primary constructor parameters
    private readonly ShardState _state = ShardState.Create(capacity, concurrencyLevel);

    // Rare: handle true 64-bit hash collisions using per-hash buckets (global map is fine; buckets are tiny)
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, long>> _hashCollisions = new(Environment.ProcessorCount, 31);

    private long _nextId; // starts at 0, Interlocked.Increment returns first id as 1

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
        var shard = _state.ShortToIdShards[ShardForShort(shortUrl)];
        if (!shard.TryGetValue(shortUrl, out var id))
        {
            return string.Empty;
        }

        var urlShard = _state.IdToUrlShards[ShardForId(id)];
        return urlShard.TryGetValue(id, out var original) ? original : string.Empty;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        long id = InternOriginal(originalUrl);
        var shard = _state.ShortToIdShards[ShardForShort(shortUrl)];
        return shard.TryAdd(shortUrl, id);
    }

    /// <inheritdoc />
    public bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue)
    {
        // expectedValue == null => key should not exist
        if (expectedValue is null)
        {
            long idNew = InternOriginal(originalUrl);
            var shard = _state.ShortToIdShards[ShardForShort(shortUrl)];
            if (shard.TryAdd(shortUrl, idNew))
            {
                currentValue = null;
                return true;
            }

            currentValue = GetCurrentOriginal(shortUrl);
            return false;
        }

        // Otherwise enforce CAS based on expected string value
        var s = _state.ShortToIdShards[ShardForShort(shortUrl)];
        if (!s.TryGetValue(shortUrl, out var existingId))
        {
            currentValue = null;
            return false;
        }

        var urlShard = _state.IdToUrlShards[ShardForId(existingId)];
        string? existingOriginal = urlShard.TryGetValue(existingId, out var s2) ? s2 : null;
        if (!string.Equals(existingOriginal, expectedValue, StringComparison.Ordinal))
        {
            currentValue = existingOriginal;
            return false;
        }

        long newId = InternOriginal(originalUrl);
        if (s.TryUpdate(shortUrl, newId, existingId))
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

        foreach (var d in _state.ShortToIdShards)
        {
            d.Clear();
        }

        foreach (var d in _state.HashToIdShards)
        {
            d.Clear();
        }

        foreach (var d in _state.IdToUrlShards)
        {
            d.Clear();
        }

        _hashCollisions.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetHash64(string value)
    {
        const ulong seed = 0xC0FFEEUL;

        // Encode as UTF-8 with minimal allocation; fall back to pooled buffer for larger strings.
        ReadOnlySpan<char> chars = value.AsSpan();
        int byteCount = Encoding.UTF8.GetByteCount(chars);

        if (byteCount <= Constants.MaxStackUnicodeCharByteCount)
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
    private long InternOriginal(string url)
    {
        ulong hash = GetHash64(url);

        // Fast path: hash -> id exists, verify content matches
        var hashShard = _state.HashToIdShards[ShardForHash(hash)];
        if (hashShard.TryGetValue(hash, out var id))
        {
            var urlShard = _state.IdToUrlShards[ShardForId(id)];
            if (urlShard.TryGetValue(id, out var existing) && string.Equals(existing, url, StringComparison.Ordinal))
            {
                return id;
            }

            // True 64-bit collision
            return InternCollision(hash, url);
        }

        // First claim on this hash
        long newId = Interlocked.Increment(ref _nextId);
        if (hashShard.TryAdd(hash, newId))
        {
            var urlShard = _state.IdToUrlShards[ShardForId(newId)];
            urlShard.TryAdd(newId, url);
            return newId;
        }

        // Lost race; resolve against published entry
        if (hashShard.TryGetValue(hash, out var racedId))
        {
            var urlShard = _state.IdToUrlShards[ShardForId(racedId)];
            if (urlShard.TryGetValue(racedId, out var raced) && string.Equals(raced, url, StringComparison.Ordinal))
            {
                return racedId;
            }

            return InternCollision(hash, url);
        }

        return InternCollision(hash, url);
    }

    // Handle rare hash collisions via per-hash string -> id buckets.
    private long InternCollision(ulong hash, string url)
    {
        var bucket = _hashCollisions.GetOrAdd(hash, static _ => new ConcurrentDictionary<string, long>(StringComparer.Ordinal));

        if (bucket.TryGetValue(url, out var id))
        {
            return id;
        }

        long newId = Interlocked.Increment(ref _nextId);

        var urlShard = _state.IdToUrlShards[ShardForId(newId)];
        if (!urlShard.TryAdd(newId, url) && bucket.TryGetValue(url, out var existed))
        {
            return existed;
        }

        if (bucket.TryAdd(url, newId))
        {
            return newId;
        }

        urlShard.TryRemove(newId, out _);
        return bucket[url];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? GetCurrentOriginal(string shortUrl)
    {
        var s = _state.ShortToIdShards[ShardForShort(shortUrl)];
        if (!s.TryGetValue(shortUrl, out long id))
        {
            return null;
        }

        var u = _state.IdToUrlShards[ShardForId(id)];
        return u.TryGetValue(id, out string? val) ? val : null;
    }

    // Partitioning helpers (power-of-two & bitmasking)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ShardForShort(string shortUrl)
    {
        // Use stable ordinal hash (consistent with PartitionedUrlRepository)
        int h = MemoryStringEqualityComparer.Ordinal.GetHashCode(shortUrl.AsMemory());
        return ((int)(uint)h) & _state.ShardMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ShardForHash(ulong hash) => (int)(hash & (uint)_state.ShardMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long ShardForId(long id) => id & _state.ShardMask;

    private sealed class ShardState
    {
        private ShardState(
            int shardCount,
            ConcurrentDictionary<string, long>[] shortToId,
            ConcurrentDictionary<ulong, long>[] hashToId,
            ConcurrentDictionary<long, string>[] idToUrl)
        {
            ShardCount = shardCount;
            ShardMask = shardCount - 1;
            ShortToIdShards = shortToId;
            HashToIdShards = hashToId;
            IdToUrlShards = idToUrl;
        }

        public int ShardCount { get; }

        public int ShardMask { get; }

        public ConcurrentDictionary<string, long>[] ShortToIdShards { get; }

        public ConcurrentDictionary<ulong, long>[] HashToIdShards { get; }

        public ConcurrentDictionary<long, string>[] IdToUrlShards { get; }

        public static ShardState Create(int capacity, int concurrencyHint)
        {
            const int MaxEntriesPerShardHint = 32_768;
            int desiredShards = Math.Max(1, DivRoundUp(capacity, MaxEntriesPerShardHint));
            int shardCount = NextPowerOfTwo(desiredShards);
            int perShardCapacity = Math.Max(31, DivRoundUp(capacity, shardCount));
            int perShardConcurrency = Math.Max(1, Math.Max(1, concurrencyHint) / shardCount);

            var shortToId = new ConcurrentDictionary<string, long>[shardCount];
            var hashToId = new ConcurrentDictionary<ulong, long>[shardCount];
            var idToUrl = new ConcurrentDictionary<long, string>[shardCount];

            for (int i = 0; i < shardCount; i++)
            {
                shortToId[i] = new ConcurrentDictionary<string, long>(perShardConcurrency, perShardCapacity, StringComparer.Ordinal);
                hashToId[i] = new ConcurrentDictionary<ulong, long>(perShardConcurrency, perShardCapacity);
                idToUrl[i] = new ConcurrentDictionary<long, string>(perShardConcurrency, perShardCapacity);
            }

            return new ShardState(shardCount, shortToId, hashToId, idToUrl);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DivRoundUp(int value, int divisor) => (value + divisor - 1) / divisor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NextPowerOfTwo(int x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return Math.Max(1, x + 1);
        }
    }
}
