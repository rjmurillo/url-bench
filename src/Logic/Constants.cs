namespace Logic;

internal static class Constants
{
    internal static readonly ulong Base62Power8 = 218_340_105_584_896;
    internal static readonly int MaxSlugLength = 8;

    // Stackalloc is limited to 2,096 bytes, which is below the size we hit StackOverflow at 56,320 bytes
    internal static readonly int MaxStackSize = 2_096;
}
