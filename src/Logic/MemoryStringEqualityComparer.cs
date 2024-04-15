namespace Logic;

public class MemoryStringEqualityComparer(StringComparison comparison) : IEqualityComparer<ReadOnlyMemory<char>>
{
    public static MemoryStringEqualityComparer Ordinal { get; } = new(StringComparison.Ordinal);
    public static MemoryStringEqualityComparer OrdinalIgnoreCase { get; } = new(StringComparison.OrdinalIgnoreCase);

    public MemoryStringEqualityComparer(bool ignoreCase = false)
        :this(ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
    {
    }

    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.Equals(y.Span, comparison);

    public int GetHashCode(ReadOnlyMemory<char> obj) => string.GetHashCode(obj.Span, comparison);
}