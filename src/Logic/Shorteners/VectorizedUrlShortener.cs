using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Logic.Contracts;
using Logic.Exceptions;

namespace Logic.Shorteners;

// TODO: Refactor this to separate classes for each of the hashing strategies
// TODO: Refactor to become a facade based on capabilities detected at runtime

/// <summary>
/// A URL shortener that generates short URLs using vectorized hashing algorithms.
/// Optimized for AVX-512, AVX2, SSE2, and ARM AdvSimd instruction sets.
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

        return Base62Encoder.EncodeToString(value, Constants.MaxSlugLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FlexibleHash(Span<byte> data)
    {
        // Check for AVX-512 support first (best performance)
        if (Avx512F.IsSupported
#if NET8_0_OR_GREATER
            && Vector512.IsHardwareAccelerated
#endif
            )
        {
            return ComputeHash512(data);
        }

        // Fall back to AVX2 (256-bit vectors)
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

        // Fall back to 128-bit vectors (SSE2 or ARM AdvSimd)
        if ((Sse2.IsSupported || AdvSimd.IsSupported)
#if NET8_0_OR_GREATER
            && Vector128.IsHardwareAccelerated
#endif
#pragma warning disable SA1009
            )
#pragma warning restore SA1009
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

        throw new ShortenerException(string.Empty, nameof(VectorizedUrlShortener), "Neither ARM.AdvSimd, AVX-512, AVX2 nor SSE2 is supported on this processor.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash512(Span<byte> data)
    {
        Vector512<uint> hash = Vector512<uint>.Zero;
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process 64-byte chunks using AVX-512
        for (; i <= data.Length - 64; i += 64)
        {
            var block = Vector512.Create(
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 4)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 8)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 12)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 16)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 20)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 24)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 28)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 32)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 36)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 40)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 44)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 48)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 52)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 56)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 60)));
            hash = Avx512F.Xor(hash, block);
        }

        // Handle remaining bytes using smaller vector sizes
        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash256(Span<byte> data)
    {
        Vector256<uint> hash = Vector256<uint>.Zero;
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 32; i += 32)
        {
            var block = Vector256.Create(
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 4)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 8)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 12)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 16)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 20)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 24)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 28)));
            hash = Avx2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeSseHash128(Span<byte> data)
    {
        Vector128<uint> hash = Vector128<uint>.Zero;
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 16; i += 16)
        {
            var block = Vector128.Create(
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 4)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 8)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 12)));
            hash = Sse2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeAdvSimdHash128(Span<byte> data)
    {
        Vector128<uint> hash = Vector128<uint>.Zero;
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 16; i += 16)
        {
            var block = Vector128.Create(
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 4)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 8)),
                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataRef, i + 12)));
            hash = AdvSimd.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessRemainingBytes(Span<byte> data, int startIndex)
    {
        ulong remainingHash = 0UL;
        int remaining = data.Length - startIndex;

        if (remaining == 0)
        {
            return remainingHash;
        }

        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process remaining bytes as scalar operations
        for (int i = startIndex; i < data.Length; i++)
        {
            remainingHash ^= (ulong)Unsafe.Add(ref dataRef, i) << ((i - startIndex) & 7);
        }

        return remainingHash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReduceHashToUInt64<T>(T hash)
        where T : struct
    {
        if (hash is Vector512<uint> v512)
        {
            // Extract quarters of Vector512<uint> and reduce step by step
            var lower256 = v512.GetLower();
            var upper256 = v512.GetUpper();

            // XOR the lower and upper halves to get a Vector256<uint>
            var reduced256 = Avx2.Xor(lower256, upper256);

            // Now reduce the Vector256<uint> using existing logic
            var lower128 = reduced256.GetLower();
            var upper128 = reduced256.GetUpper();
            var reduced128 = Sse2.Xor(lower128, upper128);

            // Final reduction to ulong
            reduced128 = Sse2.Xor(reduced128, Sse2.ShiftRightLogical128BitLane(reduced128, 8));
            var reduced64 = reduced128.AsUInt64();
            return reduced64.GetElement(0) ^ reduced64.GetElement(1);
        }

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
            return reduced64.GetElement(0) ^ reduced64.GetElement(1);
        }

        if (hash is Vector128<uint> v128)
        {
            // Reduce Vector128<uint> by XOR folding it down to 64 bits
            var shifted = Sse2.ShiftRightLogical128BitLane(v128, 8);
            var reduced = Sse2.Xor(v128, shifted);

            // Convert to Vector128<ulong> and extract the final result
            var reduced64 = reduced.AsUInt64();
            return reduced64.GetElement(0) ^ reduced64.GetElement(1);
        }

        throw new ShortenerException(string.Empty, "VectorizedUrlShortener", "Unsupported vector type for hashing.");
    }
}
