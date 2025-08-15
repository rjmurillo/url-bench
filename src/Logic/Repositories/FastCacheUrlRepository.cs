using FastCache;

namespace Logic.Repositories;

public class FastCacheUrlRepository : UrlRepositoryBase, IDisposable
{
    public override string GetOriginalUrl(string shortUrl)
    {
        if (Cached<string>.TryGet(shortUrl, out var originalUrl))
        {
            return originalUrl.Value;
        }

        return string.Empty;
    }
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        Cached<string>.Save(shortUrl, originalUrl, TimeSpan.MaxValue);
        return true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO: Clear the cache
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}