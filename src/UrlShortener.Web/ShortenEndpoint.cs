using Microsoft.AspNetCore.Http.HttpResults;

namespace UrlShortener.Web;

/// <summary>
/// Handles <c>POST /api/links</c>.
/// </summary>
internal static class ShortenEndpoint
{
    /// <summary>
    /// Shortens the URL in <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The request body.</param>
    /// <param name="links">The link service.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>201 with the short link, or 400 for an invalid URL.</returns>
    public static async Task<Results<Created<ShortenResponse>, ValidationProblem>> HandleAsync(
        ShortenRequest? request,
        LinkService links,
        CancellationToken cancellationToken)
    {
        if (!links.TryValidateTarget(request?.Url, out Uri? target, out string? error))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["url"] = [error!],
            });
        }

        string slug = await links.ShortenAsync(target!, cancellationToken).ConfigureAwait(false);
        Uri shortUrl = links.BuildShortUrl(slug);

        // Uri serializes as its original string; report the canonical form that was hashed and stored.
        var stored = new Uri(target!.AbsoluteUri);
        return TypedResults.Created(shortUrl, new ShortenResponse(slug, shortUrl, stored));
    }
}
