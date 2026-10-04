namespace UrlShortener.Web;

/// <summary>
/// The response body for a created short link.
/// </summary>
/// <param name="Slug">The slug.</param>
/// <param name="ShortUrl">The full short URL.</param>
/// <param name="Url">The normalized target URL.</param>
public sealed record ShortenResponse(string Slug, Uri ShortUrl, Uri Url);
