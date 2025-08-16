namespace Contracts;

/// <summary>
/// Defines a repository for storing and retrieving URL mappings between short URLs and their original URLs.
/// </summary>
public interface IUrlRepository
{
    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, null.</returns>
    string GetOriginalUrl(string shortUrl);

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully saved; otherwise, false.</returns>
    bool SaveUrlMapping(string shortUrl, string originalUrl);
}