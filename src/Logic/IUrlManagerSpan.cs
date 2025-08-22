using Logic.Contracts;

namespace Logic;

/// <summary>
/// Provides <see cref="ReadOnlySpan{T}" />-based URL shortening and retrieval operations.
/// </summary>
public interface IUrlManagerSpan : IUrlManager
{
    /// <summary>
    /// Retrieves the original URL associated with the specified short URL as a <see cref="ReadOnlySpan{Char}"/>.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up as a read-only character span.</param>
    /// <returns>The original URL as a <see cref="ReadOnlySpan{Char}"/> if found; otherwise, an empty span.</returns>
    ReadOnlySpan<char> GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl);
}