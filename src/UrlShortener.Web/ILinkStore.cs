namespace UrlShortener.Web;

/// <summary>
/// Persists slug-to-URL mappings.
/// </summary>
public interface ILinkStore
{
    /// <summary>
    /// Gets the URL stored for a slug.
    /// </summary>
    /// <param name="slug">The slug to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The stored URL, or <see langword="null"/> when the slug is unknown.</returns>
    Task<string?> GetAsync(string slug, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a mapping only if the slug is not already taken.
    /// </summary>
    /// <param name="slug">The slug to claim.</param>
    /// <param name="url">The URL to store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// <see langword="null"/> if the mapping was added; otherwise the URL already stored for <paramref name="slug"/>.
    /// </returns>
    Task<string?> TryAddAsync(string slug, string url, CancellationToken cancellationToken);
}
