namespace Logic.Contracts;

/// <summary>
/// Provides methods for shortening URLs and retrieving original URLs.
/// </summary>
public interface IUrlManager
{
    /// <summary>
    /// Shortens the specified long URL and saves the mapping to the repository using optimistic concurrency control.
    /// This method is optimized for distributed systems with high throughput while maintaining shortening safety.
    ///
    /// Key features:
    /// - Optimistic concurrency control for high throughput
    /// - Strong consistency guarantees within regions
    /// - Idempotent behavior for the same input URL
    /// - Preserves original URL integrity (no modification).
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <returns>The generated short URL.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="longUrl"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails.</exception>
    string ShortenAndSave(string longUrl);

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an <see cref="string.Empty"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shortUrl"/> is null or empty.</exception>
    string GetOriginalUrl(string shortUrl);
}
