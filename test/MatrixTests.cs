using Contracts;
using Logic;

namespace UrlConverterPermutations;

public class MatrixTests(ITestOutputHelper output)
{
    public static MatrixTheoryData MatrixData = new();

    [Theory]
    [MemberData(nameof(MatrixData))]
    public void RetrieveOriginalUrl_returns_original_url(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

        var f = new Facade(shortener, repository);

        var shortUrl = f.ShortenAndSave(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        var originalUrl = f.GetOriginalUrl(shortUrl);

        output.WriteLine($"Original URL: {originalUrl}");

        originalUrl.ShouldBe(longUrl);
    }

    [Theory]
    [MemberData(nameof(MatrixData))]
    public void ShortUrl_is_not_same_after_2_iterations(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const ulong upper = 2UL;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var hs = new HashSet<string>(StringComparer.Ordinal);
        var f = new Facade(shortener, repository);

        for (var i = 0UL; i < upper; i++)
        {
            var a = f.ShortenAndSave(longUrl);

            a.ShouldNotBeNullOrWhiteSpace();
            hs.Add(a).ShouldBeTrue($"Duplicate detected: {a}");
        }
    }
}