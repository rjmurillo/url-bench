using System.Collections.Concurrent;
using Contracts;

namespace Logic.Repositories;

public class ConcurrentUrlRepository : ReadOnlyMemoryUrlRepositoryBase
{
    private readonly ConcurrentDictionary<ReadOnlyMemory<char>, string> _urlMappings;

    public ConcurrentUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)
    {
    }

    public ConcurrentUrlRepository(int concurrencyLevel, int capacity)
    {
        _urlMappings = new ConcurrentDictionary<ReadOnlyMemory<char>, string>(concurrencyLevel, capacity, MemoryStringEqualityComparer.Ordinal);
    }

    public override string GetOriginalUrl(ReadOnlyMemory<char> shortUrl)
    {
        if (_urlMappings.TryGetValue(shortUrl, out var originalUrl))
        {
            return originalUrl;
        }

        return string.Empty;
    }

    public override bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, string originalUrl)
    {
        return _urlMappings.TryAdd(shortUrl, originalUrl);
    }
}