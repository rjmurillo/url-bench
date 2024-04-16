#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
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

        Span<byte> inputBuffer = stackalloc byte[Encoding.Unicode.GetMaxByteCount(originalUrl.Length + 10)]; // Extra space for attempts
        Span<char> charBuffer = MemoryMarshal.Cast<byte, char>(inputBuffer); // Reinterpret the byte buffer as char buffer

        var attempt = 0;
        do
        {
            originalUrl.AsSpan().CopyTo(charBuffer); // Copy original URL to buffer
            if (attempt > 0)
            {
                // Write attempt number as string to buffer right after the URL
                attempt.TryFormat(charBuffer[originalUrl.Length..], out _);
            }

            // Encode the char buffer containing the URL and attempt number into the input buffer
            var bytesUsed = Encoding.Unicode.GetBytes(charBuffer[..(originalUrl.Length + (attempt > 0 ? (int)Math.Log10(attempt) + 1 : 0))], inputBuffer);
            ReadOnlySpan<byte> inputSpan = inputBuffer[..bytesUsed];

            // Calculate hash and convert to Base62
            var shortUrl = Convert.AsBase62String(MurmurHash3.Hash32(ref inputSpan, seed)).ToString();

            if (Repository.SaveUrlMapping(shortUrl, originalUrl))
            {
                return shortUrl;
            }

            attempt++;
        } while (attempt <= 1000);

        throw new InvalidOperationException("Could not generate a unique short URL");
    }
}

#endif