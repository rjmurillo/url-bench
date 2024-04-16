#if NET8_0_OR_GREATER
using System.Text;
using Contracts;
using MurmurHash;

namespace Logic.Shorteners;


public class Murmur3HashUrlShortener(IUrlRepository urlRepository, uint seed) : UrlShortenerBase(urlRepository)
{
    public Murmur3HashUrlShortener(IUrlRepository urlRepository)
        :this(urlRepository, 0U)
    {
    }

    public override string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        var attempt = 0;
        ReadOnlySpan<byte> inputSpan = Encoding.Unicode.GetBytes(originalUrl).AsSpan();

        var shortUrl = Convert.AsBase62String(MurmurHash3.Hash32(ref inputSpan, seed)).ToString();

        while (!Repository.SaveUrlMapping(shortUrl, originalUrl))
        {
            inputSpan = Encoding.Unicode.GetBytes(originalUrl + attempt++).AsSpan();

            shortUrl = Convert.AsBase62String(MurmurHash3.Hash32(ref inputSpan, seed)).ToString();

            if (attempt > 1000)    // Arbitrary number to prevent infinite loop
            {
                throw new InvalidOperationException("Could not generate a unique short URL");
            }
        }

        return shortUrl;

    }
}

#endif