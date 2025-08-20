using Logic.Repositories;

namespace UrlConverterPermutations;

public class CompactUrlRepositoryTests
{
    [Fact]
    public void Save_and_GetOriginal_roundtrip()
    {
        var repo = new CompactUrlRepository();
        var shortUrl = "abc123";
        var original = "https://example.com/x";

        repo.SaveUrlMapping(shortUrl, original).ShouldBeTrue();
        repo.GetOriginalUrl(shortUrl).ShouldBe(original);
    }

    [Fact]
    public void GetOriginal_missing_returns_empty()
    {
        var repo = new CompactUrlRepository();
        repo.GetOriginalUrl("nope").ShouldBe(string.Empty);
    }

    [Fact]
    public void TrySave_expected_null_adds_when_absent()
    {
        var repo = new CompactUrlRepository();
        string? current;
        repo.TrySaveUrlMapping("s1", "https://a", expectedValue: null, out current).ShouldBeTrue();
        current.ShouldBeNull();
    }

    [Fact]
    public void TrySave_expected_null_fails_when_present_and_exposes_current()
    {
        var repo = new CompactUrlRepository();
        repo.SaveUrlMapping("s1", "https://a").ShouldBeTrue();

        string? current;
        repo.TrySaveUrlMapping("s1", "https://b", expectedValue: null, out current).ShouldBeFalse();
        current.ShouldBe("https://a");
    }

    [Fact]
    public void TrySave_expected_mismatch_returns_false_and_current()
    {
        var repo = new CompactUrlRepository();
        repo.SaveUrlMapping("s1", "https://a").ShouldBeTrue();

        string? current;
        repo.TrySaveUrlMapping("s1", "https://b", expectedValue: "https://other", out current).ShouldBeFalse();
        current.ShouldBe("https://a");
    }

    [Fact]
    public void TrySave_expected_match_updates_value()
    {
        var repo = new CompactUrlRepository();
        repo.SaveUrlMapping("s1", "https://a").ShouldBeTrue();

        string? current;
        repo.TrySaveUrlMapping("s1", "https://b", expectedValue: "https://a", out current).ShouldBeTrue();
        current.ShouldBeNull();

        repo.GetOriginalUrl("s1").ShouldBe("https://b");
    }

    [Fact]
    public void TrySave_expected_present_but_key_missing_returns_false()
    {
        var repo = new CompactUrlRepository();

        string? current;
        repo.TrySaveUrlMapping("missing", "https://b", expectedValue: "https://a", out current).ShouldBeFalse();
        current.ShouldBeNull();
    }
}
