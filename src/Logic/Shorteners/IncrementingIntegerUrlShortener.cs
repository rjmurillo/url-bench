using Contracts;

namespace Logic.Shorteners;

public class IncrementingIntegerUrlShortener(IUrlRepository urlRepository, ulong initialValue) : UrlShortenerBase(urlRepository)
{
    public IncrementingIntegerUrlShortener(IUrlRepository urlRepository)
        : this(urlRepository, 0UL)
    {
    }

    public override string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        var value = Interlocked.Increment(ref initialValue) - 1UL;
        var shortUrl = Convert.AsBase62String(value).ToString();

        while (!Repository.SaveUrlMapping(shortUrl, originalUrl))
        {
            shortUrl = Convert.AsBase62String(value).ToString();
        }

        return shortUrl;

    }
}