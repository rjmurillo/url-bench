using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Contracts;
using Logic.Exceptions;

namespace Logic.Shorteners;

// TODO: Refactor this to separate classes for each of the hashing strategies
// TODO: Refactor to become a facade based on capabilities detected at runtime

/// <summary>
/// A URL shortener that generates short URLs using vectorized hashing algorithms.
/// </summary>
public class VectorizedUrlShortener : IUrlShortener
{
    /// <summary>
    /// Shortens the specified original URL by generating a unique short URL using vectorized hashing.
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

        int charCount = originalUrl.Length;
        int byteCount = charCount * sizeof(char); // exact byte count

        // Decide between stackalloc and ArrayPool
        if (byteCount <= Constants.MaxStackSize)
        {
            Span<byte> inputBuffer = stackalloc byte[byteCount];
            return ProcessUrl(originalUrl, inputBuffer, charCount, byteCount);
        }

        var arrayFromPool = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var poolBuffer = arrayFromPool.AsSpan(0, byteCount);
            return ProcessUrl(originalUrl, poolBuffer, charCount, byteCount);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ProcessUrl(string originalUrl, Span<byte> bytes, int charCount, int byteCount)
    {
        // Reinterpret as char[] of EXACT length, then back to bytes of EXACT length
        var chars = MemoryMarshal.Cast<byte, char>(bytes)[..charCount];
        originalUrl.AsSpan().CopyTo(chars);

        // Only hash the bytes written
        var value = FlexibleHash(bytes[..byteCount]);

        Span<char> base62Buffer = stackalloc char[Math.Min(Base64Encoder.DetermineBufferLength(value), Constants.MaxSlugLength)];
        Base64Encoder.AsBase62String(value, ref base62Buffer);

        return base62Buffer.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FlexibleHash(Span<byte> data)
    {
        if (Avx2.IsSupported
#if NET8_0_OR_GREATER
            && Vector256.IsHardwareAccelerated
#endif
#pragma warning disable SA1009
           )
#pragma warning restore SA1009
        {
            return ComputeHash256(data);
        }

        if ((Sse2.IsSupported || AdvSimd.IsSupported)
#if NET8_0_OR_GREATER
            && Vector128.IsHardwareAccelerated
#endif
        )
        {
            if (Sse2.IsSupported)
            {
                return ComputeSseHash128(data);
            }

            if (AdvSimd.IsSupported)
            {
                return ComputeAdvSimdHash128(data);
            }
        }

        throw new ShortenerException(string.Empty, nameof(VectorizedUrlShortener), "Neither ARM.AdvSimd, AVX2 nor SSE2 is supported on this processor.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash256(Span<byte> data)
    {
        Vector256<uint> hash = Vector256<uint>.Zero;
        int i = 0;
        for (; i <= data.Length - 32; i += 32)
        {
            var block = Vector256.Create(
                BitConverter.ToUInt32(data.Slice(i, 4)),
                BitConverter.ToUInt32(data.Slice(i + 4, 4)),
                BitConverter.ToUInt32(data.Slice(i + 8, 4)),
                BitConverter.ToUInt32(data.Slice(i + 12, 4)),
                BitConverter.ToUInt32(data.Slice(i + 16, 4)),
                BitConverter.ToUInt32(data.Slice(i + 20, 4)),
                BitConverter.ToUInt32(data.Slice(i + 24, 4)),
                BitConverter.ToUInt32(data.Slice(i + 28, 4)));
            hash = Avx2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeSseHash128(Span<byte> data)
    {
        Vector128<uint> hash = Vector128<uint>.Zero;
        int i = 0;
        for (; i <= data.Length - 16; i += 16)
        {
            var block = Vector128.Create(
                BitConverter.ToUInt32(data.Slice(i, 4)),
                BitConverter.ToUInt32(data.Slice(i + 4, 4)),
                BitConverter.ToUInt32(data.Slice(i + 8, 4)),
                BitConverter.ToUInt32(data.Slice(i + 12, 4)));
            hash = Sse2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeAdvSimdHash128(Span<byte> data)
    {
        Vector128<uint> hash = Vector128<uint>.Zero;
        int i = 0;
        for (; i <= data.Length - 16; i += 16)
        {
            var block = Vector128.Create(
                BitConverter.ToUInt32(data.Slice(i, 4)),
                BitConverter.ToUInt32(data.Slice(i + 4, 4)),
                BitConverter.ToUInt32(data.Slice(i + 8, 4)),
                BitConverter.ToUInt32(data.Slice(i + 12, 4)));
            hash = AdvSimd.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReduceHashToUInt64<T>(T hash)
        where T : struct
    {
        if (hash is Vector256<uint> v256)
        {
            // Extract lower and upper halves of Vector256<uint>
            var lower = v256.GetLower();
            var upper = v256.GetUpper();

            // XOR the lower and upper parts to combine into a single Vector128<uint>
            var reduced = Sse2.Xor(lower, upper);

            // Further reduce the size by XOR folding the remaining 128-bit vector
            reduced = Sse2.Xor(reduced, Sse2.ShiftRightLogical128BitLane(reduced, 8));

            // Convert to Vector128<ulong> and extract the final result
            var reduced64 = reduced.AsUInt64();
            var result = reduced64.GetElement(0) ^ reduced64.GetElement(1);
            return result;
        }

        if (hash is Vector128<uint> v128 && Sse2.IsSupported)
        {
            // Reduce Vector128<uint> by XOR folding it down to 64 bits
            var shuffled = Sse2.Shuffle(v128, 0b_00_00_00_01);
            var reduced = Sse2.Xor(v128, shuffled);

            // Convert to Vector128<ulong> and extract the final result
            var reduced64 = reduced.AsUInt64();
            return reduced64.GetElement(0) ^ Sse2.Extract(reduced.AsUInt16(), 1);
        }

        throw new ShortenerException(string.Empty, "VectorizedUrlShortener", "Unsupported vector type for hashing.");
    }
}
