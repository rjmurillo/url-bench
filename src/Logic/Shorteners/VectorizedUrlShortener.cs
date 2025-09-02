using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Logic.Contracts;
using Logic.Exceptions;
using Logic.Hashing;

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
    private static ulong FlexibleHash(ReadOnlySpan<byte> data)
    {
        // Use tiered approach based on data size to minimize overhead
        int dataLength = data.Length;

        // For very small inputs (typical URLs), use fast scalar hashing
        if (dataLength < HashConstants.Vector128Threshold)
        {
            return FastScalarHash(data);
        }

        // For medium inputs, use 128-bit vectors if supported
        if (dataLength < HashConstants.Vector256Threshold && (Sse2.IsSupported || AdvSimd.IsSupported)
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

        // For large inputs, use 256-bit vectors if supported
        if (dataLength < HashConstants.Vector512Threshold && Avx2.IsSupported
#if NET8_0_OR_GREATER
            && Vector256.IsHardwareAccelerated
#endif
#pragma warning disable SA1009
           )
#pragma warning restore SA1009
        {
            return ComputeHash256(data);
        }

        // For very large inputs, use 512-bit vectors if supported
        if (Avx512F.IsSupported
#if NET8_0_OR_GREATER
            && Vector512.IsHardwareAccelerated
#endif
#pragma warning disable SA1009
           )
#pragma warning restore SA1009
        {
            return ComputeHash512(data);
        }

        // Fall back to 256-bit if available
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

        // Fall back to 128-bit if available
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

        // Final fallback to scalar hash
        return FastScalarHash(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FastScalarHash(ReadOnlySpan<byte> data)
    {
        // Use a simple but fast FNV-1a style hash for small data
        ulong hash = HashConstants.FnvOffsetBasis;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process 8 bytes at a time when possible
        int i = 0;
        int length = data.Length;

        // Process in 8-byte chunks for better performance
        // Guard against underflow when length < 8
        for (; i + 8 <= length; i += 8)
        {
            ulong chunk = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref dataRef, i));
            hash = (hash ^ chunk) * HashConstants.FnvPrime;
        }

        // Process remaining bytes
        for (; i < length; i++)
        {
            hash = (hash ^ Unsafe.Add(ref dataRef, i)) * HashConstants.FnvPrime;
        }

        return FinalHashMixing(hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash512(ReadOnlySpan<byte> data)
    {
        // Initialize with a non-zero seed to improve hash quality
        Vector512<uint> hash = Vector512.Create(
            HashConstants.GoldenRatioConstant,
            HashConstants.VectorSeed1,
            HashConstants.VectorSeed2,
            HashConstants.VectorSeed3,
            HashConstants.VectorSeed4,
            HashConstants.VectorSeed5,
            HashConstants.VectorSeed6,
            HashConstants.VectorSeed7,
            HashConstants.VectorSeed8,
            HashConstants.VectorSeed9,
            HashConstants.VectorSeed10,
            HashConstants.VectorSeed11,
            HashConstants.VectorSeed12,
            HashConstants.VectorSeed13,
            HashConstants.VectorSeed14,
            HashConstants.VectorSeed15);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process 64-byte chunks using AVX-512
        for (; i <= data.Length - 64; i += 64)
        {
            var block = Unsafe.ReadUnaligned<Vector512<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 6) to make mixing order-dependent
            var salt = Vector512.Create(unchecked(HashConstants.GoldenRatioConstant * (uint)(1 + (i >> 6))));
            block = Avx512F.Xor(block, salt);
            hash = Avx512F.Xor(hash, block);
        }

        // Handle remaining bytes using smaller vector sizes
        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash256(ReadOnlySpan<byte> data)
    {
        Vector256<uint> hash = Vector256.Create(
            HashConstants.GoldenRatioConstant,
            HashConstants.VectorSeed1,
            HashConstants.VectorSeed2,
            HashConstants.VectorSeed3,
            HashConstants.VectorSeed4,
            HashConstants.VectorSeed5,
            HashConstants.VectorSeed6,
            HashConstants.VectorSeed7);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 32; i += 32)
        {
            var block = Unsafe.ReadUnaligned<Vector256<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 5) to make mixing order-dependent
            var salt = Vector256.Create(unchecked(HashConstants.GoldenRatioConstant * (uint)(1 + (i >> 5))));
            block = Avx2.Xor(block, salt);
            hash = Avx2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeSseHash128(ReadOnlySpan<byte> data)
    {
        Vector128<uint> hash = Vector128.Create(
            HashConstants.GoldenRatioConstant,
            HashConstants.VectorSeed1,
            HashConstants.VectorSeed2,
            HashConstants.VectorSeed3);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 16; i += 16)
        {
            var block = Unsafe.ReadUnaligned<Vector128<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 4) to make mixing order-dependent
            var salt = Vector128.Create(unchecked(HashConstants.GoldenRatioConstant * (uint)(1 + (i >> 4))));
            block = Sse2.Xor(block, salt);
            hash = Sse2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeAdvSimdHash128(ReadOnlySpan<byte> data)
    {
        Vector128<uint> hash = Vector128.Create(
            HashConstants.GoldenRatioConstant,
            HashConstants.VectorSeed1,
            HashConstants.VectorSeed2,
            HashConstants.VectorSeed3);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 16; i += 16)
        {
            var block = Unsafe.ReadUnaligned<Vector128<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 4) to make mixing order-dependent
            var salt = Vector128.Create(unchecked(HashConstants.GoldenRatioConstant * (uint)(1 + (i >> 4))));
            block = AdvSimd.Xor(block, salt);
            hash = AdvSimd.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessRemainingBytes(ReadOnlySpan<byte> data, int startIndex)
    {
        int remaining = data.Length - startIndex;

        if (remaining == 0)
        {
            return 0;
        }

        ulong remainingHash = HashConstants.FnvOffsetBasis; // FNV-1a basis
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process remaining bytes with better mixing using FNV-1a approach
        for (int i = startIndex; i < data.Length; i++)
        {
            remainingHash ^= (ulong)Unsafe.Add(ref dataRef, i) << (((i - startIndex) & 7) * 8);
            remainingHash *= HashConstants.FnvPrime; // FNV-1a prime for better mixing
        }

        return remainingHash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReduceHashToUInt64<T>(T hash)
        where T : struct
    {
        ulong result;

        if (hash is Vector512<uint> v512)
        {
            // Extract quarters of Vector512<uint> and reduce step by step
            var lower256 = v512.GetLower();
            var upper256 = v512.GetUpper();

            // XOR the lower and upper halves to get a Vector256<uint> (architecture-agnostic)
            var reduced256 = lower256 ^ upper256;

            // Now reduce the Vector256<uint> using existing logic
            var lower128 = reduced256.GetLower();
            var upper128 = reduced256.GetUpper();
            var reduced128 = lower128 ^ upper128;

            // Final reduction to ulong using manual lane extraction
            var reduced64 = reduced128.AsUInt64();
            var element0 = reduced64.GetElement(0);
            var element1 = reduced64.GetElement(1);
            result = element0 ^ element1;
        }
        else if (hash is Vector256<uint> v256)
        {
            // Extract lower and upper halves of Vector256<uint>
            var lower = v256.GetLower();
            var upper = v256.GetUpper();

            // XOR the lower and upper parts to combine into a single Vector128<uint> (architecture-agnostic)
            var reduced = lower ^ upper;

            // Convert to Vector128<ulong> and extract elements for manual XOR folding
            var reduced64 = reduced.AsUInt64();
            var element0 = reduced64.GetElement(0);
            var element1 = reduced64.GetElement(1);
            result = element0 ^ element1;
        }
        else if (hash is Vector128<uint> v128)
        {
            // Convert to Vector128<ulong> and extract elements for manual XOR folding
            var reduced64 = v128.AsUInt64();
            var element0 = reduced64.GetElement(0);
            var element1 = reduced64.GetElement(1);
            result = element0 ^ element1;
        }
        else
        {
            throw new ShortenerException(string.Empty, nameof(VectorizedUrlShortener), $"Unsupported vector type '{hash.GetType().FullName}' for hashing.");
        }

        return FinalHashMixing(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FinalHashMixing(ulong hash)
    {
        // Apply final mixing for better avalanche effect (inspired by SplitMix64)
        hash ^= hash >> HashConstants.SplitMix64Shift1;
        hash *= HashConstants.SplitMix64Multiplier1;
        hash ^= hash >> HashConstants.SplitMix64Shift2;
        hash *= HashConstants.SplitMix64Multiplier2;
        hash ^= hash >> HashConstants.SplitMix64Shift3;

        return hash;
    }
}
