using Logic.Shorteners;
using Microsoft.Extensions.Options;
using UrlShortener.Web;

namespace UrlShortener.Web.Tests;

public sealed class LinkServiceTests
{
    private static readonly IOptions<ShortenerOptions> Options = Microsoft.Extensions.Options.Options.Create(
        new ShortenerOptions { BaseUrl = new Uri("https://go.example.test"), ApiKey = "test-api-key-0123456789" });

    [Fact]
    public async Task ShortenAsync_WhenPrimarySlugTakenByDifferentUrl_UsesNextSeedAndStaysIdempotent()
    {
        var target = new Uri("https://example.com/collides");
        string primary = new Murmur3HashUrlShortener(0U).ShortenUrl(target.AbsoluteUri);
        var store = new InMemoryLinkStore();
        store.Seed(primary, "https://example.com/someone-else");
        var service = new LinkService(store, Options);

        string first = await service.ShortenAsync(target, CancellationToken.None);
        string second = await service.ShortenAsync(target, CancellationToken.None);

        first.ShouldNotBe(primary);
        first.ShouldBe(new Murmur3HashUrlShortener(1U).ShortenUrl(target.AbsoluteUri));
        second.ShouldBe(first);
        (await service.ResolveAsync(first, CancellationToken.None)).ShouldBe(target.AbsoluteUri);
    }
}
