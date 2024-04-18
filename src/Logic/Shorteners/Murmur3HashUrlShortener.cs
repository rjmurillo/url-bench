#if NET8_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Contracts;
using Logic.Hashing;

namespace Logic.Shorteners;

public class Murmur3HashUrlShortener(uint seed)
    : IUrlShortener
{
    public Murmur3HashUrlShortener()
        : this(0U)
    {
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ProcessUrl(string originalUrl, Span<byte> inputBuffer)
    {
        var charBuffer = MemoryMarshal.Cast<byte, char>(inputBuffer); // Reinterpret the byte buffer as char buffer

        originalUrl.AsSpan().CopyTo(charBuffer); // Copy the original URL into the char buffer
        var bytesUsed = Encoding.Unicode.GetBytes(charBuffer, inputBuffer);
        ReadOnlySpan<byte> inputSpan = inputBuffer[..bytesUsed];

        // Calculate hash and convert to Base62
        var value = Murmur3.Hash64(ref inputSpan, seed);

        Span<char> base62Buffer = stackalloc char[Math.Min(Convert.DetermineBufferLength(value), Constants.MaxSlugLength)];
        Convert.AsBase62String(value, ref base62Buffer);
        return base62Buffer.ToString();
    }
}

#endif