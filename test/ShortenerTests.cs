using Contracts;
using NonBlocking;
using System.Text;
using System.Web;
using Logic.Shorteners;

namespace UrlConverterPermutations;

/// <summary>
/// Test class for URL shortener implementations.
/// </summary>
/// <param name="output">The test output helper for writing test information.</param>
public class ShortenerTests(ITestOutputHelper output)
{
    /// <summary>
    /// Gets the theory data containing all URL shortener implementations to test.
    /// </summary>
    public static ShortenerTheoryData ShortenerData = new();

    private static bool IsDeterministicHashShortener(IUrlShortener shortener)
    {
        var shortenerType = shortener.GetType();
        return shortenerType == typeof(VectorizedUrlShortener) ||
               shortenerType == typeof(Murmur3HashUrlShortener) ||
               shortenerType == typeof(Sha256HashUrlShortener);
    }

    /// <summary>
    /// Tests that null URLs are handled correctly by returning an empty string.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void LongUrl_is_null(IUrlShortener shortener)
    {
        const string? longUrl = null;
        var shortUrl = shortener.ShortenUrl(longUrl!);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that empty URLs are handled correctly by returning an empty string.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
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

    /// <summary>
    /// Tests that complex URLs with special characters are handled correctly.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Tricky_LongUrl_is_handled(IUrlShortener shortener)
    {
        string CreateTestUrl()
        {
            var baseUrl = "https://www.example.com:8080/search";
            var query = $"q={HttpUtility.UrlEncode("testing browser capabilities")}&lang=en";
            var hash = $"section1?nested=true&unicode={HttpUtility.UrlEncode("προσωπική δοκιμή")}&dataurl=data:text/plain;base64,SGVsbG8sIFdvcmxkIQ==";
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

    /// <summary>
    /// Tests that very long URLs are handled correctly.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
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

    /// <summary>
    /// Tests basic URL shortening functionality.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Shorten(IUrlShortener shortener)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        var shortUrl = shortener.ShortenUrl(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNullOrWhiteSpace();
        shortUrl.Length.ShouldBeLessThan(9);
        shortUrl.ShouldBeSubsetOf(Base62Chars);
    }

    /// <summary>
    /// Tests that the same input URL produces different outputs for non-deterministic shorteners.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void Same_LongUrl_Shortened_is_not_same(IUrlShortener shortener)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl1 = shortener.ShortenUrl(longUrl);
        var shortUrl2 = shortener.ShortenUrl(longUrl);

        output.WriteLine($"ShortUrl1: {shortUrl1}");
        output.WriteLine($"ShortUrl2: {shortUrl2}");

        Skip.If(IsDeterministicHashShortener(shortener));

        // For non-deterministic shorteners, different outputs are expected
        shortUrl1.ShouldNotBe(shortUrl2);        
    }

    /// <summary>
    /// Tests that shorteners can handle a large number of operations without producing duplicates.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void ShortUrl_is_not_same_after_1_million(IUrlShortener shortener)
    {
        const ulong upper = 1_000_000UL;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var hs = new HashSet<string>(StringComparer.Ordinal);

        if (IsDeterministicHashShortener(shortener))
        {
            // For deterministic hash functions, test that the same input produces consistent output
            var firstResult = shortener.ShortenUrl(longUrl);
            firstResult.ShouldNotBeNullOrWhiteSpace();
            
            for (var i = 0UL; i < upper; i++)
            {
                var result = shortener.ShortenUrl(longUrl);
                result.ShouldNotBeNullOrWhiteSpace();
                result.ShouldBe(firstResult); // Should always be the same for deterministic hash functions
            }
        }
        else
        {
            // For non-deterministic shorteners, test for uniqueness across many iterations
            for (var i = 0UL; i < upper; i++)
            {
                var a = shortener.ShortenUrl(longUrl);

                a.ShouldNotBeNullOrWhiteSpace();
                hs.Add(a).ShouldBeTrue($"Duplicate detected after {i} iterations: {a}");
            }
        }
    }

    /// <summary>
    /// Tests that shorteners are thread-safe when used in parallel operations.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    [Theory]
    [MemberData(nameof(ShortenerData))]
    public void ShortUrl_thread_safe(IUrlShortener shortener)
    {
        const int upper = 1_000_000;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var processorCount = Environment.ProcessorCount; // Number of logical CPUs
        var hs = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        if (IsDeterministicHashShortener(shortener))
        {
            // For deterministic hash functions, test thread safety by ensuring consistent output
            var expectedResult = shortener.ShortenUrl(longUrl);
            expectedResult.ShouldNotBeNullOrWhiteSpace();

            Parallel.For(0, upper, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, i =>
            {
                var result = shortener.ShortenUrl(longUrl);
                result.ShouldNotBeNullOrWhiteSpace();
                result.ShouldBe(expectedResult); // Should always be the same for deterministic hash functions
            });
        }
        else
        {
            // For non-deterministic shorteners, test for uniqueness in parallel execution
            Parallel.For(0, upper, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, i =>
            {
                var a = shortener.ShortenUrl(longUrl);

                a.ShouldNotBeNullOrWhiteSpace();
                hs.TryAdd(a, byte.MinValue).ShouldBeTrue($"Duplicate detected after {i} iterations: {a}");
            });
        }
    }
}