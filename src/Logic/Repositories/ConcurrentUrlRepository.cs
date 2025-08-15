using System.Collections.Concurrent;

namespace Logic.Repositories;

/// <summary>
/// A thread-safe URL repository implementation using ConcurrentDictionary for storing URL mappings.
/// </summary>
/// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
public class ConcurrentUrlRepository(int concurrencyLevel, int capacity) : UrlRepositoryBase, IDisposable
{
    private readonly ConcurrentDictionary<string, string> _urlMappings = new(concurrencyLevel, capacity, StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the ConcurrentUrlRepository class with default concurrency level and capacity.
    /// </summary>
    public ConcurrentUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)
    {
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public override string GetOriginalUrl(string shortUrl)
    {
        if (_urlMappings.TryGetValue(shortUrl, out var originalUrl))
        {
            return originalUrl;
        }

        return string.Empty;
    }

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully added; otherwise, false.</returns>
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        return _urlMappings.TryAdd(shortUrl, originalUrl);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the ConcurrentUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the ConcurrentUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _urlMappings.Clear();
        }
    }
}