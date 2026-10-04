using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UrlShortener.Web;

/// <summary>
/// A synthetic readiness check that round-trips a known sentinel entry through the <see cref="ILinkStore"/>.
/// The app is only ready when it can read the sentinel back with the expected value.
/// </summary>
/// <param name="store">The link store.</param>
public sealed class LinkStoreHealthCheck(ILinkStore store) : IHealthCheck
{
    /// <summary>The health check registration name.</summary>
    public const string Name = "link-store";

    /// <summary>
    /// The sentinel slug. The underscore is outside the base62 slug alphabet, so the sentinel can never be
    /// generated for, or resolved by, a public request.
    /// </summary>
    public const string SentinelSlug = "_health";

    /// <summary>The URL stored under <see cref="SentinelSlug"/>.</summary>
#pragma warning disable S1075 // A fixed value is the point: external checks (test/k6) compare against it.
    public const string SentinelUrl = "https://richardmurillo.com/?source=health-sentinel";
#pragma warning restore S1075

    private readonly ILinkStore _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await _store.GetAsync(SentinelSlug, cancellationToken).ConfigureAwait(false);
            if (stored is null)
            {
                // First start against an empty store: seed the sentinel, then require it to read back.
                await _store.TryAddAsync(SentinelSlug, SentinelUrl, cancellationToken).ConfigureAwait(false);
                stored = await _store.GetAsync(SentinelSlug, cancellationToken).ConfigureAwait(false);
            }

            return string.Equals(stored, SentinelUrl, StringComparison.Ordinal)
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context?.Registration.FailureStatus ?? HealthStatus.Unhealthy, "Sentinel entry has an unexpected value.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // Any storage failure means the app is not ready; the exception is reported, not swallowed.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new HealthCheckResult(context?.Registration.FailureStatus ?? HealthStatus.Unhealthy, "Link store is unreachable.", ex);
        }
    }
}
