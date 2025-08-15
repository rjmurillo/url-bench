namespace Logic;

internal static class Constants
{
    public const long Base62Power8 = 218_340_105_584_896;
    public const int MaxSlugLength = 8;
    // Stackalloc is limited to 2,096 bytes, which is below the size we hit StackOverflow at 56,320 bytes
    public const int MaxStackSize = 2_096;
}