using Contracts;

namespace Logic;

/// <summary>
/// Provides a simplified interface for URL shortening operations by combining a URL shortener and repository.
/// </summary>
/// <param name="shortener">The URL shortener to use for generating short URLs.</param>
/// <param name="repository">The repository to use for storing URL mappings.</param>
public class Facade(IUrlShortener? shortener, IUrlRepository? repository)
{
    private readonly IUrlShortener _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
    private readonly IUrlRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Shortens the specified long URL and saves the mapping to the repository.
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <returns>The generated short URL.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the URL mapping cannot be saved.</exception>
    public string ShortenAndSave(string longUrl)
    {
        var shortUrl = _shortener.ShortenUrl(longUrl);
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