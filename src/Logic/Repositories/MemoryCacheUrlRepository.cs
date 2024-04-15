//public class MemoryCacheUrlRepository : IUrlRepository
//{
//    private readonly IMemoryCache _cache;

//    public MemoryCacheUrlRepository()
//        : this(Constants.Base62Power7)
//    {
//    }

//    public MemoryCacheUrlRepository(long sizeLimit)
//    {
//        var options = new MemoryCacheOptions()
//        {
//            //SizeLimit = sizeLimit,
//            TrackLinkedCacheEntries = false,
//            TrackStatistics = false,
//            ExpirationScanFrequency = TimeSpan.MaxValue,
//        };

//        _cache = new MemoryCache(options);
//    }

//    public ReadOnlySpan<char> GetOriginalUrl(ReadOnlySpan<char> shortUrl)
//    {
//        _cache.TryGetValue(shortUrl, out ReadOnlyMemory<char>? originalUrl);
//        return originalUrl.AsSpan() ?? ReadOnlySpan<char>.Empty;
//    }

//    public bool SaveUrlMapping(ReadOnlySpan<char> shortUrl, ReadOnlySpan<char> originalUrl)
//    {
//        _cache.Set(shortUrl, originalUrl);
//        return true;
//    }
//}