namespace Contracts;

/// <summary>
/// Defines a contract for shortening URLs.
/// </summary>
public interface IUrlShortener
{
    /// <summary>
    /// Produces a short, slug-only identifier for the given URL.
    /// </summary>
    /// <param name="originalUrl">
    /// The original (long) URL to shorten. Any string is accepted; null, empty, or whitespace returns a <see cref="string.Empty"/> result.
    /// </param>
    /// <returns>
    /// A case-sensitive slug.
    /// The value contains no scheme, host, query, fragment, or path prefix and has no leading or trailing slashes.
    /// The slug is not URL-encoded and is intended to be used as a single path segment
    /// (e.g., compose the full URL externally via $"https://sho.rt/{slug}").
    /// Returns <see cref="string.Empty"/> if <paramref name="originalUrl"/> is null, empty, or whitespace.
    /// </returns>
    /// <remarks>
    /// - Deterministic: the same <paramref name="originalUrl"/> must always yield the same slug.
    /// - Collision semantics: different inputs may theoretically collide; collision handling is the responsibility of the caller/repository layer.
    /// - Implementations should be pure and side effect free (no I/O) and safe for concurrent use.
    /// - Implementations are expected to produce compact slugs (typically ≤ 8 characters), but the exact length is implementation-defined.
    /// </remarks>
    string ShortenUrl(string? originalUrl);
}
