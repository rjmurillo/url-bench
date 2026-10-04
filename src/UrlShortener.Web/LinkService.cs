using System.Buffers;
using System.Globalization;
using Logic.Contracts;
using Logic.Shorteners;
using Microsoft.Extensions.Options;

namespace UrlShortener.Web;

/// <summary>
/// Creates and resolves short links.
/// </summary>
/// <param name="store">The link store.</param>
/// <param name="options">The shortener options.</param>
public sealed class LinkService(ILinkStore store, IOptions<ShortenerOptions> options)
{
    /// <summary>The maximum accepted length of a target URL.</summary>
    public const int MaxUrlLength = 2048;

    /// <summary>The maximum accepted length of a slug.</summary>
    public const int MaxSlugLength = 16;

    // Each attempt hashes with a different Murmur3 seed. The probe order is deterministic, so a URL that
    // collided on an earlier seed is found again on the same later seed, which keeps shortening idempotent.
    private static readonly IUrlShortener[] Shorteners =
        [.. Enumerable.Range(0, 8).Select(static seed => new Murmur3HashUrlShortener((uint)seed))];

    private static readonly SearchValues<char> SlugChars =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");

    private readonly ILinkStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly ShortenerOptions _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

    /// <summary>
    /// Validates a candidate target URL.
    /// </summary>
    /// <param name="url">The candidate URL.</param>
    /// <param name="target">The parsed URL when valid.</param>
    /// <param name="error">A description of the problem when invalid.</param>
    /// <returns><see langword="true"/> if the URL can be shortened.</returns>
    public bool TryValidateTarget(string? url, out Uri? target, out string? error)
    {
        target = null;
        if (string.IsNullOrWhiteSpace(url) || url.Length > MaxUrlLength)
        {
            error = string.Create(CultureInfo.InvariantCulture, $"url is required and must be at most {MaxUrlLength} characters.");
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)
            || (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
                && !string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)))
        {
            error = "url must be an absolute http or https URL.";
            return false;
        }

        if (_options.BaseUrl is not null
            && string.Equals(parsed.IdnHost, _options.BaseUrl.IdnHost, StringComparison.OrdinalIgnoreCase))
        {
            error = "url must not point at the shortener itself.";
            return false;
        }

        target = parsed;
        error = null;
        return true;
    }

    /// <summary>
    /// Shortens a validated URL and stores the mapping.
    /// </summary>
    /// <param name="target">The URL to shorten.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The slug for <paramref name="target"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when every probe collides with a different URL.</exception>
    public async Task<string> ShortenAsync(Uri target, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        string url = target.AbsoluteUri;

        foreach (IUrlShortener shortener in Shorteners)
        {
            string slug = shortener.ShortenUrl(url);
            string? existing = await _store.TryAddAsync(slug, url, cancellationToken).ConfigureAwait(false);
            if (existing is null || string.Equals(existing, url, StringComparison.Ordinal))
            {
                return slug;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique slug.");
    }

    /// <summary>
    /// Resolves a slug to its target URL.
    /// </summary>
    /// <param name="slug">The slug.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The target URL, or <see langword="null"/> when the slug is malformed or unknown.</returns>
    public Task<string?> ResolveAsync(string slug, CancellationToken cancellationToken)
    {
        return IsWellFormedSlug(slug) ? _store.GetAsync(slug, cancellationToken) : Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Builds the public short URL for a slug.
    /// </summary>
    /// <param name="slug">The slug.</param>
    /// <returns>The absolute short URL.</returns>
    public Uri BuildShortUrl(string slug) => new(_options.BaseUrl!, slug);

    private static bool IsWellFormedSlug(string slug)
    {
        return slug.Length is > 0 and <= MaxSlugLength && !slug.AsSpan().ContainsAnyExcept(SlugChars);
    }
}
