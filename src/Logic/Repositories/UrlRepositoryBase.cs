using Contracts;

namespace Logic.Repositories;

/// <summary>
/// Base class for URL repository implementations that provides common functionality.
/// </summary>
public abstract class UrlRepositoryBase : IUrlRepository
{
    /// <summary>
    /// When implemented in a derived class, retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, null.</returns>
    public abstract string GetOriginalUrl(string shortUrl);

    /// <summary>
    /// When implemented in a derived class, saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully saved; otherwise, false.</returns>
    public abstract bool SaveUrlMapping(string shortUrl, string originalUrl);
}