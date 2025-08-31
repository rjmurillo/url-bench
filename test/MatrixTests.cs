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

        originalUrl.ShouldBe(longUrl);
    }

    [Theory]
    [MemberData(nameof(MatrixData))]
    public void Shortened_slug_has_no_NUL(IUrlShortener? shortener, IUrlRepository? repository)
    {
        const string input = "https://a.co/x";

        Skip.If(shortener is null, "Shortener is not initialized.");
        repository.ShouldNotBeNull();

        var shortUrl = shortener!.ShortenUrl(input);

        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldNotBeEmpty();
        shortUrl.IndexOf('\0').ShouldBeLessThan(0);
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
        repository!.GetOriginalUrl(shortUrl.AsSpan()).ShouldBe(original);
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
    public void GetOriginal_missing_span_returns_empty(IUrlShortener? shortener, IUrlRepository? repository)
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

    /// <summary>
    /// Tests that URL shorteners handle very short URLs without buffer underflow or crashes.
    /// This specifically tests the fix for the buffer underflow bug where URLs shorter than 8 bytes
    /// could cause integer underflow in the loop condition (i &lt;= length - 8).
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    /// <param name="_">The URL repository to test.</param>
    [Theory]
    [MemberData(nameof(MatrixData))]
    public void ShortenUrl_very_short_urls_no_buffer_underflow(IUrlShortener? shortener, IUrlRepository? _)
    {
        Skip.If(shortener is null, "Shortener is not initialized.");

        // Test edge cases that could trigger buffer underflow
        var testCases = new[]
        {
            string.Empty,           // 0 characters (0 bytes)
            "a",          // 1 character (2 bytes as UTF-16)
            "ab",         // 2 characters (4 bytes as UTF-16)
            "abc",        // 3 characters (6 bytes as UTF-16)
            "abcd",       // 4 characters (8 bytes as UTF-16) - boundary case
            "x",          // Single character
            "🚀",         // Single emoji (4 bytes as UTF-16 surrogate pair)
            "a.co",       // Minimal domain-like URL
            "x.y", // Another minimal case
        };

        foreach (var testUrl in testCases)
        {
            // Should not crash, throw exceptions, or cause buffer overflow
            var result = shortener!.ShortenUrl(testUrl);

            // Verify result is safe and reasonable
            result.ShouldNotBeNull($"Shortener returned null for input: '{testUrl}'");

            // Empty input should return empty result
            if (string.IsNullOrWhiteSpace(testUrl))
            {
                result.ShouldBeEmpty($"Expected empty result for empty/whitespace input: '{testUrl}'");
            }
            else
            {
                result.ShouldNotBeEmpty($"Expected non-empty result for non-empty input: '{testUrl}'");
                result.IndexOf('\0').ShouldBeLessThan(0, $"Result should not contain NUL characters for input: '{testUrl}'");
            }
        }
    }
}
