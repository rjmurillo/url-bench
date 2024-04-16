#if NET8_0_OR_GREATER

using System.Runtime.InteropServices;
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

        Span<byte> urlBuffer = stackalloc byte[Encoding.Unicode.GetMaxByteCount(originalUrl.Length + 10)]; // Extra space for attempts
        Span<byte> hashBuffer = stackalloc byte[32]; // SHA256 outputs 32 bytes
        Span<char> base62Buffer = stackalloc char[8]; // Buffer for Base62 result


        var attempt = 0;
        string shortUrl;
        do
        {
            var bytes = Encoding.Unicode.GetBytes(originalUrl.AsSpan(), urlBuffer);
            if (attempt > 0)
            {
                // Append the attempt number as Unicode characters directly to the buffer
                Span<char> charBuffer = MemoryMarshal.Cast<byte, char>(urlBuffer);
                charBuffer[originalUrl.Length] = (char)('0' + (attempt % 10)); // Simplistic, works for attempt < 10
                bytes = Encoding.Unicode.GetBytes(charBuffer.Slice(0, originalUrl.Length + 1), urlBuffer);
            }

            sha256.TryComputeHash(urlBuffer.Slice(0, bytes), hashBuffer, out _);
            Convert.AsBase62String(hashBuffer, ref base62Buffer);

            shortUrl = new string(base62Buffer);

            if (Repository.SaveUrlMapping(shortUrl, originalUrl))
                break;

            attempt++;
        } while (attempt <= 10);

        if (attempt > 10)
        {
            throw new InvalidOperationException("Could not generate a unique short URL");
        }

        return shortUrl;
    }
}

#endif