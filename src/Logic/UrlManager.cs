using Contracts;

namespace Logic;

/// <summary>
/// Provides a simplified interface for URL shortening operations by combining a URL shortener and repository.
/// </summary>
/// <param name="shortener">The URL shortener to use for generating short URLs.</param>
/// <param name="repository">The repository to use for storing URL mappings.</param>
public sealed class UrlManager(IUrlShortener? shortener, IUrlRepository? repository)
{
    private readonly IUrlShortener _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
    private readonly IUrlRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Shortens the specified long URL and saves the mapping to the repository.
    /// This method is pure with respect to the input URL: the slug is derived only from <paramref name="longUrl"/>.
    /// If a collision occurs (the generated slug already maps to a different URL), an exception is thrown.
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <returns>The generated short URL.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the generated slug is already mapped to a different URL.</exception>
    public string ShortenAndSave(string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
        {
            return string.Empty;
        }

        var shortUrl = _shortener.ShortenUrl(longUrl);
        if (string.IsNullOrEmpty(shortUrl))
        {
            return string.Empty;
        }

        // Check existing mapping for idempotency and collision detection
        var existing = _repository.GetOriginalUrl(shortUrl);
        if (!string.IsNullOrEmpty(existing))
        {
            if (string.Equals(existing, longUrl, StringComparison.Ordinal))
            {
                // Idempotent: same input returns the same slug
                return shortUrl;
            }

            // Collision: slug already mapped to a different original URL
            throw new InvalidOperationException("Collision detected: generated short URL is already mapped to a different original URL.");
        }

        // No existing mapping; save and return
        if (_repository.SaveUrlMapping(shortUrl, longUrl))
        {
            return shortUrl;
        }

        throw new InvalidOperationException("Failed to save URL mapping.");
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public string GetOriginalUrl(string shortUrl)
    {
        return _repository.GetOriginalUrl(shortUrl) ?? string.Empty;
    }
}
