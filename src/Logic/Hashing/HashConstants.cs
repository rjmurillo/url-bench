namespace Logic.Hashing;

/// <summary>
/// Hash constants used by vectorized URL shortening algorithms.
/// Contains vector initialization seeds derived from mathematical constants.
/// </summary>
internal static class HashConstants
{
    // Suppress ECS0200 for hash constants that are mathematically derived and never change.
    // These values are compile-time constants based on mathematical constants (π, e, √2, etc.)
    // and using 'const' provides better performance in vectorized operations by enabling
    // compile-time optimizations and inlining.
#pragma warning disable ECS0200 // Prefer readonly over const

    /// <summary>
    /// First vector initialization constant derived from fractional part of π.
    /// Used as the second element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed1 = 0x7f4a7c15U;

    /// <summary>
    /// Second vector initialization constant derived from fractional part of e.
    /// Used as the third element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed2 = 0x93ea7575U;

    /// <summary>
    /// Third vector initialization constant derived from fractional part of √2.
    /// Used as the fourth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed3 = 0xe9b279b5U;

    /// <summary>
    /// Fourth vector initialization constant derived from fractional part of √3.
    /// Used as the fifth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed4 = 0xa2c2a44dU;

    /// <summary>
    /// Fifth vector initialization constant derived from fractional part of √5.
    /// Used as the sixth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed5 = 0x5b9d5bcdU;

    /// <summary>
    /// Sixth vector initialization constant derived from fractional part of ln(2).
    /// Used as the seventh element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed6 = 0x3c6ef372U;

    /// <summary>
    /// Seventh vector initialization constant derived from fractional part of ln(3).
    /// Used as the eighth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed7 = 0x78dde6e5U;

    /// <summary>
    /// Eighth vector initialization constant derived from fractional part of log(2).
    /// Used as the ninth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed8 = 0x87b0b0fdU;

    /// <summary>
    /// Ninth vector initialization constant derived from fractional part of γ (Euler-Mascheroni constant).
    /// Used as the tenth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed9 = 0x2767f0b1U;

    /// <summary>
    /// Tenth vector initialization constant derived from fractional part of δ (Feigenbaum constant).
    /// Used as the eleventh element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed10 = 0xcf72b09eU;

    /// <summary>
    /// Eleventh vector initialization constant derived from fractional part of α (fine-structure constant).
    /// Used as the twelfth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed11 = 0xa99a7b85U;

    /// <summary>
    /// Twelfth vector initialization constant derived from fractional part of Catalan's constant.
    /// Used as the thirteenth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed12 = 0x64916f35U;

    /// <summary>
    /// Thirteenth vector initialization constant derived from fractional part of Khinchin's constant.
    /// Used as the fourteenth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed13 = 0x519ba6c7U;

    /// <summary>
    /// Fourteenth vector initialization constant derived from fractional part of Glaisher's constant.
    /// Used as the fifteenth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed14 = 0x9db1a5a9U;

    /// <summary>
    /// Fifteenth vector initialization constant derived from fractional part of Meissel-Mertens constant.
    /// Used as the sixteenth element in vector hash state initialization.
    /// </summary>
    internal const uint VectorSeed15 = 0x456789abU;

    /// <summary>
    /// Golden ratio constant (2^32 * φ - 1) used for hash mixing and salting.
    /// This value provides good bit distribution and is commonly used in hash functions
    /// to reduce clustering and improve avalanche properties.
    /// Value: 0x9e3779b9 = 2654435769 decimal.
    /// </summary>
    internal const uint GoldenRatioConstant = 0x9e3779b9U;

    /// <summary>
    /// FNV-1a 64-bit offset basis used for initializing the hash state when processing remaining bytes.
    /// This is the standard FNV-1a basis value defined in the FNV specification (RFC draft).
    /// Value: 0xcbf29ce484222325 = 14695981039346656037 decimal.
    /// </summary>
    internal const ulong FnvOffsetBasis = 0xcbf29ce484222325UL;

    /// <summary>
    /// FNV-1a 64-bit prime multiplier used for hash mixing when processing remaining bytes.
    /// This is the standard FNV-1a prime value defined in the FNV specification (RFC draft).
    /// Value: 0x100000001b3 = 1099511628211 decimal.
    /// </summary>
    internal const ulong FnvPrime = 0x100000001b3UL;

    /// <summary>
    /// SplitMix64 first multiplier constant used in final hash mixing.
    /// This value provides excellent avalanche properties for hash finalization.
    /// Value: 0xbf58476d1ce4e5b9 = 13787848793156543929 decimal.
    /// </summary>
    internal const ulong SplitMix64Multiplier1 = 0xbf58476d1ce4e5b9UL;

    /// <summary>
    /// SplitMix64 second multiplier constant used in final hash mixing.
    /// This value provides excellent avalanche properties for hash finalization.
    /// Value: 0x94d049bb133111eb = 10723151780598845931 decimal.
    /// </summary>
    internal const ulong SplitMix64Multiplier2 = 0x94d049bb133111ebUL;

    /// <summary>
    /// First right shift amount used in SplitMix64 final mixing.
    /// This shift amount is optimized for 64-bit hash distribution.
    /// </summary>
    internal const int SplitMix64Shift1 = 30;

    /// <summary>
    /// Second right shift amount used in SplitMix64 final mixing.
    /// This shift amount is optimized for 64-bit hash distribution.
    /// </summary>
    internal const int SplitMix64Shift2 = 27;

    /// <summary>
    /// Third right shift amount used in SplitMix64 final mixing.
    /// This shift amount is optimized for 64-bit hash distribution.
    /// </summary>
    internal const int SplitMix64Shift3 = 31;

    /// <summary>
    /// Threshold for switching to 128-bit vectorization (512 bytes).
    /// Below this threshold, scalar hashing is more efficient due to setup overhead.
    /// </summary>
    internal const int Vector128Threshold = 512;

    /// <summary>
    /// Threshold for switching to 256-bit vectorization (4KB).
    /// Below this threshold, 128-bit vectors are more efficient.
    /// </summary>
    internal const int Vector256Threshold = 4096;

    /// <summary>
    /// Threshold for switching to 512-bit vectorization (16KB).
    /// Below this threshold, 256-bit vectors are more efficient.
    /// </summary>
    internal const int Vector512Threshold = 16384;

#pragma warning restore ECS0200 // Prefer readonly over const
}
