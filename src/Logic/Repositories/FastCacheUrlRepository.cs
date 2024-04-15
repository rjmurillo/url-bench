using Contracts;
using FastCache;

namespace Logic.Repositories;

public class FastCacheUrlRepository : ReadOnlyMemoryUrlRepositoryBase
{
    public override string GetOriginalUrl(ReadOnlyMemory<char> shortUrl)
    {
        if (Cached<ReadOnlyMemory<char>>.TryGet(shortUrl, out var originalUrl))
        {
            return originalUrl.Value.Span;
        }

        return string.Empty;
    }
    public override bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, ReadOnlyMemory<char> originalUrl)
    {
        Cached<ReadOnlyMemory<char>>.Save(shortUrl, originalUrl, TimeSpan.MaxValue);
        return true;
    }
}