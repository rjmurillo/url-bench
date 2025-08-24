namespace Logic.Contracts;

/// <summary>
/// Defines a contract for a URL repository that supports retrieval of the original URL using a <see cref="ReadOnlySpan{Char}"/>.
/// </summary>
public interface IUrlRepositorySpan : IUrlRepository
{
    /// <summary>
    /// Retrieves the original URL associated with the specified short URL as a <see cref="string"/>.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up as a <see cref="ReadOnlySpan{T}" />.</param>
    /// <returns>The original URL as a <see cref="string"/> if found; otherwise, <see cref="string.Empty"/>.</returns>
    string GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl);
}
