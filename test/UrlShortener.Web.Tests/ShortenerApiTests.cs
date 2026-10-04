using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Web;

namespace UrlShortener.Web.Tests;

public sealed class ShortenerApiTests : IDisposable
{
    private const string ApiKey = "test-api-key-0123456789";
    private const string BaseUrl = "https://go.example.test";

    private readonly InMemoryLinkStore _store = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ShortenerApiTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Shortener:ApiKey", ApiKey);
            builder.UseSetting("Shortener:BaseUrl", BaseUrl);
            builder.UseSetting("ConnectionStrings:tables", "UseDevelopmentStorage=true");
            builder.UseSetting("Aspire:Azure:Data:Tables:DisableHealthChecks", "true");
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<ILinkStore>(_store)));
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key-0123456789")]
    public async Task Shorten_WithoutValidApiKey_ReturnsUnauthorized(string? key)
    {
        using HttpResponseMessage response = await PostAsync("https://example.com/", key);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Shorten_ThenResolve_RedirectsToOriginalUrl()
    {
        const string target = "https://example.com/some/long/path?q=1";

        using HttpResponseMessage created = await PostAsync(target, ApiKey);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        ShortenResponse? body = await created.Content.ReadFromJsonAsync<ShortenResponse>();
        body.ShouldNotBeNull();
        body.ShortUrl.ShouldBe(new Uri($"{BaseUrl}/{body.Slug}"));
        created.Headers.Location.ShouldBe(body.ShortUrl);

        using HttpResponseMessage redirect = await _client.GetAsync(new Uri($"/{body.Slug}", UriKind.Relative));
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        redirect.Headers.Location.ShouldBe(new Uri(target));
    }

    [Fact]
    public async Task Shorten_SameUrlTwice_ReturnsSameSlug()
    {
        using HttpResponseMessage first = await PostAsync("https://example.com/a", ApiKey);
        using HttpResponseMessage second = await PostAsync("https://example.com/a", ApiKey);

        ShortenResponse? a = await first.Content.ReadFromJsonAsync<ShortenResponse>();
        ShortenResponse? b = await second.Content.ReadFromJsonAsync<ShortenResponse>();
        a!.Slug.ShouldBe(b!.Slug);
    }

    [Fact]
    public async Task Shorten_MixedCaseSchemeAndHost_ReturnsStoredCanonicalUrl()
    {
        using HttpResponseMessage response = await PostAsync("HTTPS://EXAMPLE.COM/Path?q=1", ApiKey);

        ShortenResponse? body = await response.Content.ReadFromJsonAsync<ShortenResponse>();
        body!.Url.OriginalString.ShouldBe("https://example.com/Path?q=1");
        _store.Peek(body.Slug).ShouldBe("https://example.com/Path?q=1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://go.example.test/abc")]
    public async Task Shorten_WithInvalidUrl_ReturnsBadRequest(string url)
    {
        using HttpResponseMessage response = await PostAsync(url, ApiKey);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resolve_UnknownSlug_ReturnsNotFound()
    {
        using HttpResponseMessage response = await _client.GetAsync(new Uri("/Zz9", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("has-dash")]
    [InlineData("toolongslug0123456789")]
    public async Task Resolve_MalformedSlug_ReturnsNotFoundWithoutStoreLookup(string slug)
    {
        using HttpResponseMessage response = await _client.GetAsync(new Uri($"/{slug}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _store.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task Health_WithReachableStore_SeedsSentinelAndReturnsOk()
    {
        using HttpResponseMessage response = await _client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _store.Peek(LinkStoreHealthCheck.SentinelSlug).ShouldBe(LinkStoreHealthCheck.SentinelUrl);
    }

    [Fact]
    public async Task Health_WithWrongSentinelValue_ReturnsServiceUnavailable()
    {
        _store.Seed(LinkStoreHealthCheck.SentinelSlug, "https://example.com/tampered");

        using HttpResponseMessage response = await _client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_WithUnreachableStore_ReturnsServiceUnavailableButStaysAlive()
    {
        _store.Failure = new HttpRequestException("storage down");

        using HttpResponseMessage health = await _client.GetAsync(new Uri("/health", UriKind.Relative));
        using HttpResponseMessage alive = await _client.GetAsync(new Uri("/alive", UriKind.Relative));

        health.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        alive.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Resolve_SentinelSlug_ReturnsNotFound()
    {
        _store.Seed(LinkStoreHealthCheck.SentinelSlug, LinkStoreHealthCheck.SentinelUrl);

        using HttpResponseMessage response = await _client.GetAsync(new Uri($"/{LinkStoreHealthCheck.SentinelSlug}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> PostAsync(string url, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/links", UriKind.Relative))
        {
            Content = JsonContent.Create(new { url }),
        };
        if (key is not null)
        {
            request.Headers.Add(ApiKeyEndpointFilter.HeaderName, key);
        }

        return _client.SendAsync(request);
    }
}
