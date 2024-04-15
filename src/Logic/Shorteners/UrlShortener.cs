using Contracts;
using Logic.Repositories;

namespace Logic.Shorteners;

public class ReadOnlyMemoryUrlShortener(ReadOnlyMemoryUrlRepositoryBase urlRepository, ulong initialValue) 
    : ReadOnlyMemoryUrlShortenerBase(urlRepository)
{
    public ReadOnlyMemoryUrlShortener(ReadOnlyMemoryUrlRepositoryBase urlRepository)
        : this(urlRepository, 0UL)
    {
    }

    public ReadOnlyMemoryUrlShortener(IUrlRepository urlRepository)
        : this((urlRepository as ReadOnlyMemoryUrlRepositoryBase)!, 0UL)
    {
    }

    public override ReadOnlySpan<char> ShortenUrl(ReadOnlyMemory<char> originalUrl)
    {
        if (!originalUrl.IsEmpty && !originalUrl.Span.IsWhiteSpace())
        {
            var value = Interlocked.Increment(ref initialValue) - 1UL;
            var shortUrl = Convert.AsBase62String(value);

            // TODO: Be smarter about this so we don't cause infinite loop when repository is down
            while (!Repository.SaveUrlMapping(shortUrl.ToArray(), originalUrl))
            {
                shortUrl = Convert.AsBase62String(value);
            }

            return shortUrl;
        }

        return ReadOnlySpan<char>.Empty;
    }
}

public class UrlShortener(IUrlRepository urlRepository, ulong initialValue) : UrlShortenerBase(urlRepository)
{
    public UrlShortener(IUrlRepository urlRepository)
        : this(urlRepository, 0UL)
    {
    }

    public override string ShortenUrl(string originalUrl)
    {
        if (!string.IsNullOrWhiteSpace(originalUrl))
        {
            var value = Interlocked.Increment(ref initialValue) - 1UL;
            var shortUrl = Convert.AsBase62String(value).ToString();

            while (!Repository.SaveUrlMapping(shortUrl, originalUrl))
            {
                shortUrl = Convert.AsBase62String(value).ToString();
            }

            return shortUrl;
        }

        return string.Empty;
    }
}