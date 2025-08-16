using Logic;
using Logic.Repositories;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// A URL repository implementation using ASP.NET Core's IMemoryCache for storing URL mappings.
/// </summary>
public class MemoryCacheUrlRepository : UrlRepositoryBase, IDisposable
{
    private readonly IMemoryCache _cache;
    private static readonly MemoryCacheEntryOptions Options = new() { Size = 1 };

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheUrlRepository"/> class with default size limit.
    /// </summary>
    public MemoryCacheUrlRepository()
        : this(Constants.Base62Power8)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheUrlRepository"/> class with the specified size limit.
    /// </summary>
    /// <param name="sizeLimit">The maximum number of entries the cache can hold.</param>
    public MemoryCacheUrlRepository(long sizeLimit)
    {
        var options = new MemoryCacheOptions()
        {
            SizeLimit = sizeLimit,
            TrackLinkedCacheEntries = false,
            TrackStatistics = false,
            ExpirationScanFrequency = TimeSpan.MaxValue,
        };

        _cache = new MemoryCache(options);
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public override string GetOriginalUrl(string shortUrl)
    {
        _cache.TryGetValue(shortUrl, out string? originalUrl);
        return originalUrl ?? string.Empty;
    }

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>Always returns true as the operation is assumed to succeed.</returns>
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        _cache.Set(shortUrl, originalUrl, Options);
        return true;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the MemoryCacheUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache.Dispose();
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the MemoryCacheUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
