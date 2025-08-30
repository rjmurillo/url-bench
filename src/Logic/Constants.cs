namespace Logic;

/// <summary>
/// Contains constants used throughout the URL shortening logic.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// The maximum size for stack allocation. Stackalloc is limited to 2,096 bytes, which is below the size we hit StackOverflow at 56,320 bytes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ECS0200:Consider using readonly instead of const for better flexibility", Justification = "Value is a compile-time constant and const is preferred for performance and clarity.")]
    internal const int MaxStackSize = 2_096;

    /// <summary>
    /// The number of bytes per Unicode character (UTF-16 encoding).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ECS0200:Consider using readonly instead of const for better flexibility", Justification = "Value is a compile-time constant and const is preferred for performance and clarity.")]
    internal const int UnicodeCharByteCount = 2;

    /// <summary>
    /// The maximum number of Unicode characters that can be stack-allocated within MaxStackSize.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ECS0200:Consider using readonly instead of const for better flexibility", Justification = "Value is a compile-time constant and const is preferred for performance and clarity.")]
    internal const int MaxStackUnicodeCharByteCount = MaxStackSize / UnicodeCharByteCount;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 1-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power1 = 62;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 2-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power2 = 3_844;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 3-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power3 = 238_328;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 4-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power4 = 14_776_336;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 5-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power5 = 916_132_832;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 6-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power6 = 56_800_235_584;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 7-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power7 = 3_521_614_606_208;

    /// <summary>
    /// The maximum number of unique URLs that can be generated with 8-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power8 = 218_340_105_584_896;

    /// <summary>
    /// The maximum length of a short URL slug.
    /// </summary>
    internal static readonly int MaxSlugLength = 8;
}
