namespace UrlShortener.Web;

/// <summary>
/// The request body for creating a short link.
/// </summary>
/// <param name="Url">The URL to shorten.</param>
public sealed record ShortenRequest(string? Url);
