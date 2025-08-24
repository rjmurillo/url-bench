using Logic;
using Logic.Contracts;

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

    [Theory]
    [MemberData(nameof(MatrixData))]
    public void RetrieveOriginalUrl_returns_original_url_with_Span(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

        var f = new UrlManager(shortener, repository);

        var shortUrl = f.ShortenAndSave(longUrl);

        output.WriteLine($"Shortened URL: {shortUrl}");

        var originalUrl = f.GetOriginalUrl(shortUrl.AsSpan());

        output.WriteLine($"Original URL: {originalUrl}");

        originalUrl.ToString().ShouldBe(longUrl);
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

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void Save_and_GetOriginal_roundtrip(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const string shortUrl = "abc123";
        const string original = "https://example.com/x";

        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository!.SaveUrlMapping(shortUrl, original).ShouldBeTrue();
        repository!.GetOriginalUrl(shortUrl).ShouldBe(original);
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void Save_and_GetOriginal_Span_roundtrip(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const string shortUrl = "abc123";
        const string original = "https://example.com/x";

        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository!.SaveUrlMapping(shortUrl, original).ShouldBeTrue();
        repository!.GetOriginalUrl(shortUrl).ShouldBe(original);
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void GetOriginal_missing_returns_empty(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository!.GetOriginalUrl("nope").ShouldBe(string.Empty);
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void GetOriginal_missing_returns_empty_span(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository!.GetOriginalUrl("nope".AsSpan()).ShouldBe(string.Empty);
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void TrySave_expected_null_adds_when_absent(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository.TrySaveUrlMapping("s1", "https://a", expectedValue: null, out var current).ShouldBeTrue();
        current.ShouldBeNull();
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void TrySave_expected_null_fails_when_present_and_exposes_current(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository.SaveUrlMapping("s1", "https://a").ShouldBeTrue();

        repository.TrySaveUrlMapping("s1", "https://b", expectedValue: null, out var current).ShouldBeFalse();
        current.ShouldBe("https://a");
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void TrySave_expected_mismatch_returns_false_and_current(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository.SaveUrlMapping("s1", "https://a").ShouldBeTrue();

        repository.TrySaveUrlMapping("s1", "https://b", expectedValue: "https://other", out var current).ShouldBeFalse();
        current.ShouldBe("https://a");
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void TrySave_expected_match_updates_value(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository.SaveUrlMapping("s1", "https://a").ShouldBeTrue();

        repository.TrySaveUrlMapping("s1", "https://b", expectedValue: "https://a", out var current).ShouldBeTrue();
        current.ShouldBeNull();

        repository.GetOriginalUrl("s1").ShouldBe("https://b");
    }

    [SkippableTheory]
    [MemberData(nameof(MatrixData))]
    public void TrySave_expected_present_but_key_missing_returns_false(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null || repository is null, "Shortener or repository is not initialized.");

        repository.TrySaveUrlMapping("missing", "https://b", expectedValue: "https://a", out var current).ShouldBeFalse();
        current.ShouldBeNull();
    }
}
