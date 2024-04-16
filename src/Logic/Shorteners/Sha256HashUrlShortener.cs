using System.Security.Cryptography;
using System.Text;
using Contracts;

namespace Logic.Shorteners;

public class Sha256HashUrlShortener(IUrlRepository urlRepository) : UrlShortenerBase(urlRepository)
{
    private SHA256 sha256 = SHA256.Create();

    public override string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        var attempt = 0;
        var shortUrl = Convert.AsBase62String(sha256.ComputeHash(Encoding.Unicode.GetBytes(originalUrl))).ToString();

        while (!Repository.SaveUrlMapping(shortUrl, originalUrl))
        {
            shortUrl = Convert.AsBase62String(sha256.ComputeHash(Encoding.Unicode.GetBytes(originalUrl + attempt++))).ToString();

            if (attempt > 1000)    // Arbitrary number to prevent infinite loop
            {
                throw new InvalidOperationException("Could not generate a unique short URL");
            }
        }

        return shortUrl;

    }
}