using Logic;
using Logic.Repositories;
using Microsoft.Extensions.Caching.Memory;

public class MemoryCacheUrlRepository : UrlRepositoryBase, IDisposable
{
    private readonly IMemoryCache _cache;
    private static readonly MemoryCacheEntryOptions Options = new() { Size = 1 };

    public MemoryCacheUrlRepository()
        : this(Constants.Base62Power8)
    {
    }

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

    public override string GetOriginalUrl(string shortUrl)
    {
        _cache.TryGetValue(shortUrl, out string? originalUrl);
        return originalUrl ?? string.Empty;
    }

    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        _cache.Set(shortUrl, originalUrl, Options);
        return true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}