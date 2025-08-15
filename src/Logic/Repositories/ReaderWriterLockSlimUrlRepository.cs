namespace Logic.Repositories;

/// <summary>
/// A thread-safe URL repository implementation using ReaderWriterLockSlim for synchronization.
/// </summary>
/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
public class ReaderWriterLockSlimUrlRepository(int capacity) : UrlRepositoryBase, IDisposable
{
    // We're using this because ConcurrentDictionary is too slow
    private readonly ReaderWriterLockSlim _lock = new();

    private readonly Dictionary<string, string> _urlMappings = new(capacity, StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the ReaderWriterLockSlimUrlRepository class with default capacity.
    /// </summary>
    public ReaderWriterLockSlimUrlRepository()
        : this(short.MaxValue)
    {
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public override string GetOriginalUrl(string shortUrl)
    {
        _lock.EnterReadLock();
        try
        {
            if (_urlMappings.TryGetValue(shortUrl, out var originalUrl))
            {
                return originalUrl;
            }

            return string.Empty;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully added; otherwise, false.</returns>
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        _lock.EnterWriteLock();
        try
        {
            // Upsert semantics to avoid failing on duplicate keys
            _urlMappings[shortUrl] = originalUrl;
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the ReaderWriterLockSlimUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _urlMappings.Clear();
            _lock.Dispose();
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the ReaderWriterLockSlimUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}