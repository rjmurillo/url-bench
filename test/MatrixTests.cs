using Contracts;
using Logic;

namespace UrlConverterPermutations;

/// <summary>
/// Test class for testing combinations of URL shorteners and repositories.
/// </summary>
/// <param name="output">The test output helper for writing test information.</param>
public class MatrixTests(ITestOutputHelper output)
{
    /// <summary>
    /// Gets the theory data containing all combinations of URL shorteners and repositories to test.
    /// </summary>
    public static MatrixTheoryData MatrixData = new();

    /// <summary>
    /// Tests that the original URL can be retrieved after shortening and saving.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    /// <param name="repository">The URL repository to test.</param>
    [Theory]
    [MemberData(nameof(MatrixData))]
    public void RetrieveOriginalUrl_returns_original_url(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

        var f = new UrlManager(shortener, repository);

        var shortUrl = f.ShortenAndSave(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        var originalUrl = f.GetOriginalUrl(shortUrl);

        output.WriteLine($"Original URL: {originalUrl}");

        originalUrl.ShouldBe(longUrl);
    }

    /// <summary>
    /// Tests that different short URLs are generated for the same input URL across multiple iterations.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    /// <param name="repository">The URL repository to test.</param>
    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void ShortUrl_is_not_same_after_2_iterations(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const ulong upper = 2UL;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var hs = new HashSet<string>(StringComparer.Ordinal);
        var f = new UrlManager(shortener, repository);

        Skip.If(Utilities.IsDeterministicHashShortener(shortener), "Deterministic hash shorteners are not supported.");

        for (var i = 0UL; i < upper; i++)
        {
            var a = f.ShortenAndSave(longUrl);

            a.ShouldNotBeNullOrWhiteSpace();
            hs.Add(a).ShouldBeTrue($"Duplicate detected: {a}");
        }
    }
}