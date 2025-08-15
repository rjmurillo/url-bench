using FastCache;

namespace Logic.Repositories;

/// <summary>
/// A URL repository implementation using FastCache for storing URL mappings with expiration support.
/// </summary>
public class FastCacheUrlRepository : UrlRepositoryBase, IDisposable
{
    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public override string GetOriginalUrl(string shortUrl)
    {
        if (Cached<string>.TryGet(shortUrl, out var originalUrl))
        {
            return originalUrl.Value;
        }

        return string.Empty;
    }
    
    /// <summary>
    /// Saves a mapping between a short URL and its original URL with no expiration.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>Always returns true as the operation is assumed to succeed.</returns>
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        Cached<string>.Save(shortUrl, originalUrl, TimeSpan.MaxValue);
        return true;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the FastCacheUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO: Clear the cache
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the FastCacheUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}