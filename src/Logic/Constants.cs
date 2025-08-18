namespace Logic;

/// <summary>
/// Contains constants used throughout the URL shortening logic.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// The maximum number of unique URLs that can be generated with 8-character base62 slugs.
    /// </summary>
    internal static readonly ulong Base62Power8 = 218_340_105_584_896;

    /// <summary>
    /// The maximum length of a short URL slug.
    /// </summary>
    internal static readonly int MaxSlugLength = 8;

    /// <summary>
    /// The maximum size for stack allocation. Stackalloc is limited to 2,096 bytes, which is below the size we hit StackOverflow at 56,320 bytes.
    /// </summary>
    internal static readonly int MaxStackSize = 2_096;
}
