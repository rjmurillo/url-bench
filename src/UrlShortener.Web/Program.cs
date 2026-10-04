using Microsoft.Extensions.Options;
using UrlShortener.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableServiceClient("tables");

builder.Services.AddOptions<ShortenerOptions>()
    .BindConfiguration(ShortenerOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<ILinkStore, TableLinkStore>();
builder.Services.AddSingleton<LinkService>();
builder.Services.AddSingleton<ApiKeyEndpointFilter>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<LinkStoreHealthCheck>(LinkStoreHealthCheck.Name, tags: ["ready"], timeout: TimeSpan.FromSeconds(5));

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

app.MapPost("/api/links", ShortenEndpoint.HandleAsync)
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .DisableAntiforgery();

app.MapGet("/{slug}", async Task<IResult> (string slug, LinkService links, CancellationToken cancellationToken) =>
{
    string? target = await links.ResolveAsync(slug, cancellationToken).ConfigureAwait(false);
    return target is null ? TypedResults.NotFound() : TypedResults.Redirect(target);
});

app.MapGet("/", (IOptions<ShortenerOptions> options) =>
    options.Value.HomeUrl is { } home ? Results.Redirect(home.AbsoluteUri) : Results.NotFound());

await app.RunAsync().ConfigureAwait(false);
