using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Web;

/// <summary>
/// Configuration for the URL shortener web application, bound from the <c>Shortener</c> section.
/// </summary>
public sealed class ShortenerOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Shortener";

    /// <summary>
    /// Gets or sets the public base URL used to compose short links, for example <c>https://go.richardmurillo.com</c>.
    /// </summary>
    [Required]
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the API key required to create short links.
    /// </summary>
    [Required]
    [MinLength(16)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional URL to redirect requests for the site root to.
    /// </summary>
    public Uri? HomeUrl { get; set; }
}
