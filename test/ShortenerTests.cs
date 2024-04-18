using Contracts;
using NonBlocking;
using Shouldly;

using System.Text;
using System.Web;

using Xunit.Abstractions;

namespace UrlConverterPermutations;

public class ShortenerTests(ITestOutputHelper output)
{
    public static ShortenerTheoryData ShortenerData = new();

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void LongUrl_is_null(IUrlShortener shortener)
    {
        const string longUrl = null;
        var shortUrl = shortener.ShortenUrl(longUrl!);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void LongUrl_is_empty(IUrlShortener shortener)
    {
        var longUrl = string.Empty;
        var shortUrl = shortener.ShortenUrl(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Tricky_LongUrl_is_handled(IUrlShortener shortener)
    {
        string CreateTestUrl()
        {
            string baseUrl = "https://www.example.com:8080/search";
            string query = $"q={HttpUtility.UrlEncode("testing browser capabilities")}&lang=en";
            string hash = $"section1?nested=true&unicode={HttpUtility.UrlEncode("προσωπική δοκιμή")}&dataurl=data:text/plain;base64,SGVsbG8sIFdvcmxkIQ==";
            return $"{baseUrl}?{query}#{hash}";
        }

        var Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        var longUrl = CreateTestUrl();
        var shortUrl = shortener.ShortenUrl(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldNotBeEmpty();
        shortUrl.Length.ShouldBeLessThan(9);
        shortUrl.ShouldBeSubsetOf(Base62Chars);
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Long_LongUrl_is_handled(IUrlShortener shortener)
    {
        string CreateTestUrl()
        {
            var sb = new StringBuilder("https://www.example.com?");
            sb.Append("data=");

            // Create a large string of dummy data
            var longData = new string('x', 4096); // Create a string with 4096 'x' characters

            sb.Append(longData);

            return sb.ToString();
        }

        var Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        var longUrl = CreateTestUrl();
        var shortUrl = shortener.ShortenUrl(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldNotBeEmpty();
        shortUrl.Length.ShouldBeLessThan(9);
        shortUrl.ShouldBeSubsetOf(Base62Chars);
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Shorten(IUrlShortener shortener)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        var shortUrl = shortener.ShortenUrl(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.Length.ShouldBeLessThan(9);
        shortUrl.ShouldBeSubsetOf(Base62Chars);
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Same_LongUrl_Shortened_is_not_same(IUrlShortener shortener)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl1 = shortener.ShortenUrl(longUrl);
        var shortUrl2 = shortener.ShortenUrl(longUrl);

        output.WriteLine($"ShortUrl1: {shortUrl1}");
        output.WriteLine($"ShortUrl2: {shortUrl2}");

        shortUrl1.ShouldNotBe(shortUrl2);
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void ShortUrl_is_not_same_after_10_million(IUrlShortener shortener)
    {
        const ulong upper = 10_000_000UL;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var hs = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0UL; i < upper; i++)
        {
            var a = shortener.ShortenUrl(longUrl);

            a.ShouldNotBeNullOrWhiteSpace();
            hs.Add(a).ShouldBeTrue($"Duplicate detected: {a}");
        }
    }

    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void ShortUrl_thread_safe(IUrlShortener shortener)
    {
        const int upper = 1_000_000;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var processorCount = Environment.ProcessorCount; // Number of logical CPUs
        var hs = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        Parallel.For(0, upper, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, i =>
        {
            var a = shortener.ShortenUrl(longUrl);

            a.ShouldNotBeNullOrWhiteSpace();
            hs.TryAdd(a, byte.MinValue).ShouldBeTrue($"Duplicate detected after {i} iterations: {a}");
        });
    }


}