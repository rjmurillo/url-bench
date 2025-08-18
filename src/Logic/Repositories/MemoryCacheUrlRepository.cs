using Microsoft.Extensions.Caching.Memory;

namespace Logic.Repositories;

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
    public MemoryCacheUrlRepository(ulong sizeLimit)
    {
        var options = new MemoryCacheOptions()
        {
            SizeLimit = (long)sizeLimit,
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
    /// Attempts to save a URL mapping using optimistic concurrency control.
    /// Note: MemoryCache doesn't support conditional operations, so this is a simplified implementation.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <param name="expectedValue">The expected current value. If null, the key should not exist.</param>
    /// <param name="currentValue">When this method returns, contains the current value if the save failed due to a condition mismatch; otherwise, null.</param>
    /// <returns>true if the save operation was successful; otherwise, false.</returns>
    public override bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue)
    {
        // MemoryCache doesn't support conditional operations, so we implement a simplified version
        if (expectedValue is null)
        {
            // Check if key already exists
            if (_cache.TryGetValue(shortUrl, out string? existing))
            {
                currentValue = existing;
                return false;
            }

            // Key doesn't exist, save it
            var retVal = SaveUrlMapping(shortUrl, originalUrl);
            currentValue = null;
            return retVal;
        }

        // Check if current value matches expected
        if (_cache.TryGetValue(shortUrl, out string? current))
        {
            if (string.Equals(current, expectedValue, StringComparison.Ordinal))
            {
                // Update the value
                _cache.Set(shortUrl, originalUrl, Options);
                currentValue = null;
                return true;
            }
            else
            {
                // Value doesn't match expected
                currentValue = current;
                return false;
            }
        }
        else
        {
            // Key doesn't exist but we expected it to
            currentValue = null;
            return false;
        }
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
