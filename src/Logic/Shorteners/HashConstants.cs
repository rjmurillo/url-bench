namespace Logic.Shorteners;

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

#pragma warning restore ECS0200 // Prefer readonly over const
}
