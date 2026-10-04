using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace UrlShortener.Web;

/// <summary>
/// Rejects requests whose <c>X-Api-Key</c> header does not match the configured API key.
/// </summary>
public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    /// <summary>The request header that carries the API key.</summary>
    public const string HeaderName = "X-Api-Key";

    private readonly byte[] _expectedHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyEndpointFilter"/> class.
    /// </summary>
    /// <param name="options">The shortener options.</param>
    public ApiKeyEndpointFilter(IOptions<ShortenerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.ApiKey));
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string? provided = context.HttpContext.Request.Headers[HeaderName];
        if (string.IsNullOrEmpty(provided))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        // Hashing both sides gives equal-length inputs, so the comparison time does not leak the key length.
        byte[] providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        if (!CryptographicOperations.FixedTimeEquals(providedHash, _expectedHash))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        return next(context);
    }
}
