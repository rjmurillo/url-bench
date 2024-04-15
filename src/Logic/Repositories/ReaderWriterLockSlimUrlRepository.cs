namespace Logic.Repositories;

public class ReaderWriterLockSlimReadOnlyMemoryRepository(int capacity) : ReadOnlyMemoryUrlRepositoryBase
{
    // We're using this because ConcurrentDictionary is too slow
    private readonly ReaderWriterLockSlim _lock = new();

    private readonly Dictionary<ReadOnlyMemory<char>, string> _urlMappings = new(capacity, MemoryStringEqualityComparer.Ordinal);

    public ReaderWriterLockSlimReadOnlyMemoryRepository()
        : this(short.MaxValue)
    {
    }

    public override string GetOriginalUrl(ReadOnlyMemory<char> shortUrl)
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

    public override bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, string originalUrl)
    {
        _lock.EnterWriteLock();
        try
        {
            return _urlMappings.TryAdd(shortUrl, originalUrl);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}

public class ReaderWriterLockSlimUrlRepository(int capacity) : UrlRepositoryBase
{
    // We're using this because ConcurrentDictionary is too slow
    private readonly ReaderWriterLockSlim _lock = new();

    private readonly Dictionary<string, string> _urlMappings = new(capacity, StringComparer.Ordinal);

    public ReaderWriterLockSlimUrlRepository()
        : this(short.MaxValue)
    {
    }

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

    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        _lock.EnterWriteLock();
        try
        {
            return _urlMappings.TryAdd(shortUrl, originalUrl);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}