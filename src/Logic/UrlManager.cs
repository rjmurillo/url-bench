using Contracts;

namespace Logic;

/// <summary>
/// Provides a simplified interface for URL shortening operations by combining a URL shortener and repository.
/// Optimized for distributed systems with high throughput and strong consistency guarantees.
/// </summary>
/// <param name="shortener">The URL shortener to use for generating short URLs.</param>
/// <param name="repository">The repository to use for storing URL mappings.</param>
public sealed class UrlManager(
    IUrlShortener? shortener,
    IUrlRepository? repository)
{
    private readonly IUrlShortener _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
    private readonly IUrlRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

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
    public string ShortenAndSave(string longUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(longUrl);

        // Generate short URL
        var shortUrl = _shortener.ShortenUrl(longUrl);

        // Try optimistic save (expecting no existing mapping)
        var success = _repository.TrySaveUrlMapping(shortUrl, longUrl, expectedValue: null, out var currentValue);

        if (success)
        {
            // Successfully saved new mapping
            return shortUrl;
        }

        // Check if the collision is with the same URL (idempotency)
        if (currentValue is not null && string.Equals(currentValue, longUrl, StringComparison.Ordinal))
        {
            // Idempotent case: same input returns the same slug
            return shortUrl;
        }

        // Collision with different URL - this is a failure case
        throw new InvalidOperationException($"Collision detected for short URL '{shortUrl}'. Existing URL: '{currentValue}', New URL: '{longUrl}'");
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shortUrl"/> is null or empty.</exception>
    public string GetOriginalUrl(string shortUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortUrl);
        return _repository.GetOriginalUrl(shortUrl);
    }
}
