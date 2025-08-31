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
    /// Golden ratio constant (2^32 * φ - 1) used for hash mixing and salting.
    /// This value provides good bit distribution and is commonly used in hash functions
    /// to reduce clustering and improve avalanche properties.
    /// Value: 0x9e3779b9 = 2654435769 decimal.
    /// </summary>
#pragma warning disable ECS0200 // Prefer readonly over const
    private const uint GoldenRatioConstant = 0x9e3779b9U;
#pragma warning restore ECS0200 // Prefer readonly over const

    /// <summary>
    /// FNV-1a 64-bit offset basis used for initializing the hash state when processing remaining bytes.
    /// This is the standard FNV-1a basis value defined in the FNV specification (RFC draft).
    /// Value: 0xcbf29ce484222325 = 14695981039346656037 decimal.
    /// </summary>
#pragma warning disable ECS0200 // Prefer readonly over const
    private const ulong FnvOffsetBasis = 0xcbf29ce484222325UL;
#pragma warning restore ECS0200 // Prefer readonly over const

    /// <summary>
    /// FNV-1a 64-bit prime multiplier used for hash mixing when processing remaining bytes.
    /// This is the standard FNV-1a prime value defined in the FNV specification (RFC draft).
    /// Value: 0x100000001b3 = 1099511628211 decimal.
    /// </summary>
#pragma warning disable ECS0200 // Prefer readonly over const
    private const ulong FnvPrime = 0x100000001b3UL;
#pragma warning restore ECS0200 // Prefer readonly over const

    /// <summary>
    /// Threshold for switching to 128-bit vectorization (512 bytes).
    /// Below this threshold, scalar hashing is more efficient due to setup overhead.
    /// </summary>
    private const int Vector128Threshold = 512;

    /// <summary>
    /// Threshold for switching to 256-bit vectorization (4KB).
    /// Below this threshold, 128-bit vectors are more efficient.
    /// </summary>
    private const int Vector256Threshold = 4096;

    /// <summary>
    /// Threshold for switching to 512-bit vectorization (16KB).
    /// Below this threshold, 256-bit vectors are more efficient.
    /// </summary>
    private const int Vector512Threshold = 16384;

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
        // Use tiered approach based on data size to minimize overhead
        int dataLength = data.Length;

        // For very small inputs (typical URLs), use fast scalar hashing
        if (dataLength < Vector128Threshold)
        {
            return FastScalarHash(data);
        }

        // For medium inputs, use 128-bit vectors if supported
        if (dataLength < Vector256Threshold && (Sse2.IsSupported || AdvSimd.IsSupported)
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
        if (dataLength < Vector512Threshold && Avx2.IsSupported
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
    private static ulong FastScalarHash(Span<byte> data)
    {
        // Use a simple but fast FNV-1a style hash for small data
        ulong hash = FnvOffsetBasis;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process 8 bytes at a time when possible
        int i = 0;
        int length = data.Length;

        // Process in 8-byte chunks for better performance
        // Guard against underflow when length < 8
        for (; i + 8 <= length; i += 8)
        {
            ulong chunk = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref dataRef, i));
            hash = (hash ^ chunk) * FnvPrime;
        }

        // Process remaining bytes
        for (; i < length; i++)
        {
            hash = (hash ^ Unsafe.Add(ref dataRef, i)) * FnvPrime;
        }

        return FinalHashMixing(hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash512(Span<byte> data)
    {
        // Initialize with a non-zero seed to improve hash quality
        Vector512<uint> hash = Vector512.Create(
            GoldenRatioConstant,
            0x7f4a7c15U,
            0x93ea7575U,
            0xe9b279b5U,
            0xa2c2a44dU,
            0x5b9d5bcdU,
            0x3c6ef372U,
            0x78dde6e5U,
            0x87b0b0fdU,
            0x2767f0b1U,
            0xcf72b09eU,
            0xa99a7b85U,
            0x64916f35U,
            0x519ba6c7U,
            0x9db1a5a9U,
            0x456789abU);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process 64-byte chunks using AVX-512
        for (; i <= data.Length - 64; i += 64)
        {
            var block = Unsafe.ReadUnaligned<Vector512<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 6) to make mixing order-dependent
            var salt = Vector512.Create(unchecked(GoldenRatioConstant * (uint)(1 + (i >> 6))));
            block = Avx512F.Xor(block, salt);
            hash = Avx512F.Xor(hash, block);
        }

        // Handle remaining bytes using smaller vector sizes
        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeHash256(Span<byte> data)
    {
        Vector256<uint> hash = Vector256.Create(
            GoldenRatioConstant,
            0x7f4a7c15U,
            0x93ea7575U,
            0xe9b279b5U,
            0xa2c2a44dU,
            0x5b9d5bcdU,
            0x3c6ef372U,
            0x78dde6e5U);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 32; i += 32)
        {
            var block = Unsafe.ReadUnaligned<Vector256<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 5) to make mixing order-dependent
            var salt = Vector256.Create(unchecked(GoldenRatioConstant * (uint)(1 + (i >> 5))));
            block = Avx2.Xor(block, salt);
            hash = Avx2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeSseHash128(Span<byte> data)
    {
        Vector128<uint> hash = Vector128.Create(
            GoldenRatioConstant,
            0x7f4a7c15U,
            0x93ea7575U,
            0xe9b279b5U);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 16; i += 16)
        {
            var block = Unsafe.ReadUnaligned<Vector128<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 4) to make mixing order-dependent
            var salt = Vector128.Create(unchecked(GoldenRatioConstant * (uint)(1 + (i >> 4))));
            block = Sse2.Xor(block, salt);
            hash = Sse2.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeAdvSimdHash128(Span<byte> data)
    {
        Vector128<uint> hash = Vector128.Create(
            GoldenRatioConstant,
            0x7f4a7c15U,
            0x93ea7575U,
            0xe9b279b5U);
        int i = 0;
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        for (; i <= data.Length - 16; i += 16)
        {
            var block = Unsafe.ReadUnaligned<Vector128<uint>>(ref Unsafe.Add(ref dataRef, i));

            // Salt with block index (i >> 4) to make mixing order-dependent
            var salt = Vector128.Create(unchecked(GoldenRatioConstant * (uint)(1 + (i >> 4))));
            block = AdvSimd.Xor(block, salt);
            hash = AdvSimd.Xor(hash, block);
        }

        return ReduceHashToUInt64(hash) ^ ProcessRemainingBytes(data, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessRemainingBytes(Span<byte> data, int startIndex)
    {
        int remaining = data.Length - startIndex;

        if (remaining == 0)
        {
            return 0;
        }

        ulong remainingHash = FnvOffsetBasis; // FNV-1a basis
        ref byte dataRef = ref MemoryMarshal.GetReference(data);

        // Process remaining bytes with better mixing using FNV-1a approach
        for (int i = startIndex; i < data.Length; i++)
        {
            remainingHash ^= (ulong)Unsafe.Add(ref dataRef, i) << (((i - startIndex) & 7) * 8);
            remainingHash *= FnvPrime; // FNV-1a prime for better mixing
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

            // XOR the lower and upper halves to get a Vector256<uint>
            var reduced256 = Avx2.Xor(lower256, upper256);

            // Now reduce the Vector256<uint> using existing logic
            var lower128 = reduced256.GetLower();
            var upper128 = reduced256.GetUpper();
            var reduced128 = Sse2.Xor(lower128, upper128);

            // Final reduction to ulong
            reduced128 = Sse2.Xor(reduced128, Sse2.ShiftRightLogical128BitLane(reduced128, 8));
            var reduced64 = reduced128.AsUInt64();
            result = reduced64.GetElement(0) ^ reduced64.GetElement(1);
        }
        else if (hash is Vector256<uint> v256)
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
            result = reduced64.GetElement(0) ^ reduced64.GetElement(1);
        }
        else if (hash is Vector128<uint> v128)
        {
            // Reduce Vector128<uint> by XOR folding it down to 64 bits
            var shifted = Sse2.ShiftRightLogical128BitLane(v128, 8);
            var reduced = Sse2.Xor(v128, shifted);

            // Convert to Vector128<ulong> and extract the final result
            var reduced64 = reduced.AsUInt64();
            result = reduced64.GetElement(0) ^ reduced64.GetElement(1);
        }
        else
        {
            throw new ShortenerException(string.Empty, "VectorizedUrlShortener", "Unsupported vector type for hashing.");
        }

        return FinalHashMixing(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FinalHashMixing(ulong hash)
    {
        // Apply final mixing for better avalanche effect (inspired by SplitMix64)
        hash ^= hash >> 30;
        hash *= 0xbf58476d1ce4e5b9UL;
        hash ^= hash >> 27;
        hash *= 0x94d049bb133111ebUL;
        hash ^= hash >> 31;

        return hash;
    }
}
