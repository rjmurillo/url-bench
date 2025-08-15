using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

using Contracts;

namespace Logic.Shorteners;

/// <summary>
/// A URL shortener that generates short URLs using the SHA-256 hash algorithm.
/// </summary>
public class Sha256HashUrlShortener()
    : IUrlShortener, IDisposable
{
    private SHA256 _sha256 = SHA256.Create();
    private bool _disposed;

    /// <summary>
    /// Shortens the specified original URL by generating a unique short URL using the SHA-256 hash algorithm.
    /// </summary>
    /// <param name="originalUrl">The original (long) URL to shorten. May be any valid URL string.</param>
    /// <returns>
    /// A case-sensitive slug consisting exclusively of characters.
    /// The value contains no scheme, host, or path prefix and has no leading or trailing slashes.
    /// The slug is not URL-encoded and is intended to be used as a single path segment
    /// (e.g., compose the full URL externally via $"https://sho.rt/{slug}").
    /// Returns an empty string if <paramref name="originalUrl"/> is null, empty, or whitespace.
    /// </returns>
    public string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        var byteCount = Encoding.Unicode.GetMaxByteCount(originalUrl.Length);

        // Decide between stackalloc and ArrayPool
        if (byteCount <= Constants.MaxStackSize)
        {
            Span<byte> inputBuffer = stackalloc byte[byteCount];
            return ProcessUrl(originalUrl, inputBuffer);
        }

        var arrayFromPool = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var poolBuffer = arrayFromPool.AsSpan(0, byteCount);
            return ProcessUrl(originalUrl, poolBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    // This is unlikely to inline (too many IL bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ProcessUrl(string originalUrl, Span<byte> inputBuffer)
    {
        // Copy the original URL into the buffer
        var bytesUsed = Encoding.Unicode.GetBytes(originalUrl, inputBuffer);

        // SHA256 outputs 32 bytes, 256 bits
        Span<byte> hashBuffer = stackalloc byte[32];
        var success = _sha256.TryComputeHash(inputBuffer[..bytesUsed], hashBuffer, out _);

        Debug.Assert(success, "Could not create the hash from input.");

        // Base62 encode the SHA256 hash
        Span<char> base62Buffer = stackalloc char[Constants.MaxSlugLength];
        Convert.AsBase62String(hashBuffer, ref base62Buffer);

        return base62Buffer.ToString();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the Sha256HashUrlShortener and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _sha256?.Dispose();
            _disposed = true;
        }
    }
}