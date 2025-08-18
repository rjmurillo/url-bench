namespace Logic;

/// <summary>
/// Provides equality comparison for ReadOnlyMemory&lt;char&gt; using the specified string comparison.
/// </summary>
/// <param name="comparison">The string comparison to use for equality checks.</param>
public class MemoryStringEqualityComparer(StringComparison comparison) : IEqualityComparer<ReadOnlyMemory<char>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryStringEqualityComparer"/> class.
    /// </summary>
    /// <param name="ignoreCase">true to ignore case during comparison; otherwise, false.</param>
    public MemoryStringEqualityComparer(bool ignoreCase = false)
        : this(ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
    {
    }

    /// <summary>
    /// Gets a MemoryStringEqualityComparer that uses ordinal string comparison.
    /// </summary>
    public static MemoryStringEqualityComparer Ordinal { get; } = new(StringComparison.Ordinal);

    /// <summary>
    /// Gets a MemoryStringEqualityComparer that uses ordinal case-insensitive string comparison.
    /// </summary>
    public static MemoryStringEqualityComparer OrdinalIgnoreCase { get; } = new(StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether two ReadOnlyMemory&lt;char&gt; instances are equal.
    /// </summary>
    /// <param name="x">The first ReadOnlyMemory&lt;char&gt; to compare.</param>
    /// <param name="y">The second ReadOnlyMemory&lt;char&gt; to compare.</param>
    /// <returns>true if the two instances are equal; otherwise, false.</returns>
    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.Equals(y.Span, comparison);

    /// <summary>
    /// Returns a hash code for the specified ReadOnlyMemory&lt;char&gt;.
    /// </summary>
    /// <param name="obj">The ReadOnlyMemory&lt;char&gt; for which to get a hash code.</param>
    /// <returns>A hash code for the specified object.</returns>
    public int GetHashCode(ReadOnlyMemory<char> obj) => string.GetHashCode(obj.Span, comparison);
}
