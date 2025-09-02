using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Logic.Contracts;
using Logic.Hashing;

namespace Logic.Shorteners;

/// <summary>
/// A URL shortener that generates short URLs using the Murmur3 hash algorithm.
/// </summary>
/// <param name="seed">The seed value to use for the Murmur3 hash algorithm.</param>
public class Murmur3HashUrlShortener(uint seed)
    : IUrlShortener
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Murmur3HashUrlShortener"/> class with a seed value of 0.
    /// </summary>
    public Murmur3HashUrlShortener()
        : this(0U)
    {
    }

    /// <summary>
    /// Shortens the specified original URL by generating a unique short URL using the Murmur3 hash algorithm.
    /// </summary>
    /// <param name="originalUrl">The original (long) URL to shorten. May be any valid URL string.</param>
    /// <returns>
    /// A case-sensitive slug consisting exclusively of characters.
    /// The value contains no scheme, host, or path prefix and has no leading or trailing slashes.
    /// The slug is not URL-encoded and is intended to be used as a single path segment
    /// (e.g., compose the full URL externally via $"https://sho.rt/{slug}").
    /// Returns an empty string if <paramref name="originalUrl"/> is null, empty, or whitespace.
    /// </returns>
    public string ShortenUrl(string? originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        int byteCount = Encoding.UTF8.GetByteCount(originalUrl);

        // Decide between stackalloc and ArrayPool
        if (byteCount <= Constants.MaxStackSize)
        {
            Span<byte> inputBuffer = stackalloc byte[byteCount];
            return ProcessUrl(originalUrl, inputBuffer, byteCount);
        }

        byte[] arrayFromPool = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Span<byte> poolBuffer = arrayFromPool.AsSpan(0, byteCount);
            return ProcessUrl(originalUrl, poolBuffer, byteCount);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    // REVIEW: This isn't inlining: localloc size unknown
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ProcessUrl(string originalUrl, Span<byte> inputBuffer, int byteCount)
    {
        // Copy the original URL into the buffer
#pragma warning disable ECS0900 // false-positive
        int bytesUsed = Encoding.UTF8.GetBytes(originalUrl, inputBuffer);
#pragma warning restore ECS0900
        ReadOnlySpan<byte> inputSpan = inputBuffer[..bytesUsed];

        // Calculate hash and convert to Base62
        ulong value = Murmur3.Hash64(ref inputSpan, seed);

        return Base62Encoder.EncodeToString(value, Constants.MaxSlugLength);
    }
}
