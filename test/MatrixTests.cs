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

        // Test deterministic edge cases for 0-4 characters (including surrogate pairs)
        var deterministicTestCases = new[]
        {
            string.Empty,                    // 0 characters (0 bytes)
            "a",                            // 1 character (2 bytes as UTF-16)
            "ab",                           // 2 characters (4 bytes as UTF-16)
            "abc",                          // 3 characters (6 bytes as UTF-16)
            "abcd",                         // 4 characters (8 bytes as UTF-16) - boundary case
            "🚀",                           // Single emoji (4 bytes as UTF-16 surrogate pair)
            "🚀a",                          // Emoji + ASCII (6 bytes as UTF-16)
            "a🚀",                          // ASCII + emoji (6 bytes as UTF-16)
            "🚀🎯",                         // Two emojis (8 bytes as UTF-16)
        };

        foreach (var testUrl in deterministicTestCases)
        {
            // Should not crash, throw exceptions, or cause buffer overflow
            var result = shortener!.ShortenUrl(testUrl);

            // Verify result is safe and reasonable
            result.ShouldNotBeNull($"Shortener returned null for input: '{testUrl}' (length: {testUrl.Length})");

            // Empty input should return empty result
            if (string.IsNullOrWhiteSpace(testUrl))
            {
                result.ShouldBeEmpty($"Expected empty result for empty/whitespace input: '{testUrl}'");
            }
            else
            {
                result.ShouldNotBeEmpty($"Expected non-empty result for non-empty input: '{testUrl}' (length: {testUrl.Length})");
                result.IndexOf('\0').ShouldBeLessThan(0, $"Result should not contain NUL characters for input: '{testUrl}' (length: {testUrl.Length})");

                // Verify result is valid Base62 (only contains 0-9, A-Z, a-z)
                result.All(c => char.IsAsciiLetterOrDigit(c)).ShouldBeTrue($"Result should only contain Base62 characters for input: '{testUrl}' (length: {testUrl.Length})");
            }
        }
    }

    /// <summary>
    /// Tests URL shorteners with inputs at vectorized processing tier boundaries.
    /// This validates stability across tier switches for VectorizedUrlShortener:
    /// - SSE2/AdvSimd: 16-byte boundaries
    /// - AVX2: 32-byte boundaries
    /// - AVX-512: 64-byte boundaries
    /// - Stack allocation: 2,096-byte boundary.
    /// </summary>
    /// <param name="shortener">The URL shortener to test.</param>
    /// <param name="repository">The URL repository (unused for this test).</param>
    [Theory]
    [MemberData(nameof(MatrixData))]
    public void ShortenUrl_tier_boundary_stability(IUrlShortener? shortener, IUrlRepository? repository)
    {
        Skip.If(shortener is null, "Shortener is not initialized.");

        // Repository parameter is required for matrix test pattern but not used in this test
        _ = repository;

        // Test boundaries for vectorized processing tiers (measured in UTF-16 characters)
        var tierBoundaries = new[]
        {
            8,      // 16 bytes (SSE2/AdvSimd boundary)
            16,     // 32 bytes (AVX2 boundary)
            32,     // 64 bytes (AVX-512 boundary)
            1048,   // 2,096 bytes (MaxStackSize boundary)
        };

        foreach (var boundary in tierBoundaries)
        {
            // Test N-1, N, N+1 around each boundary
            var testSizes = new[] { boundary - 1, boundary, boundary + 1 };

            for (var i = 0; i < testSizes.Length; i++)
            {
                int size = testSizes[i];
                if (size <= 0)
                {
                    continue;
                }

                // Create deterministic test string of exact size
                var testUrl = new string('x', size);

                // Should not crash or produce inconsistent results
                var result1 = shortener!.ShortenUrl(testUrl);
                var result2 = shortener!.ShortenUrl(testUrl);

                // Verify consistency (deterministic shorteners should produce same result)
                if (Utilities.IsDeterministicHashShortener(shortener))
                {
                    result1.ShouldBe(result2, $"Deterministic shortener produced different results for size {size} (boundary: {boundary})");
                }

                // Verify result quality
                result1.ShouldNotBeNull($"Shortener returned null for size {size} (boundary: {boundary})");
                result1.ShouldNotBeEmpty($"Shortener returned empty for size {size} (boundary: {boundary})");
                result1.IndexOf('\0').ShouldBeLessThan(0, $"Result contains NUL for size {size} (boundary: {boundary})");

                // Verify result is valid Base62 (only contains 0-9, A-Z, a-z)
                result1.All(c => char.IsAsciiLetterOrDigit(c)).ShouldBeTrue($"Result contains invalid Base62 characters for size {size} (boundary: {boundary})");

                // Verify reasonable length (should not be excessively long)
                result1.Length.ShouldBeLessThanOrEqualTo(12, $"Result too long for size {size} (boundary: {boundary}): {result1}");
            }
        }
    }
}
