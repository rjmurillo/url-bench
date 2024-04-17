using System.Collections.Concurrent;
using Contracts;

namespace Logic.Repositories;

public class ConcurrentUrlRepository(int concurrencyLevel, int capacity) : UrlRepositoryBase
{
    private readonly ConcurrentDictionary<string, string> _urlMappings = new(concurrencyLevel, capacity, StringComparer.Ordinal);

    public ConcurrentUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)
    {
    }

    public override string GetOriginalUrl(string shortUrl)
    {
        if (_urlMappings.TryGetValue(shortUrl, out var originalUrl))
        {
            return originalUrl;
        }

        return string.Empty;
    }

    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        return _urlMappings.TryAdd(shortUrl, originalUrl);
    }
}