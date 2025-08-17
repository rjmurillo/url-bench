using Contracts;
using Logic.CollisionResolution;
using Logic.Contracts;
using Logic.Retry;
using Polly;

namespace Logic;

/// <summary>
/// Provides a simplified interface for URL shortening operations by combining a URL shortener and repository.
/// Optimized for distributed systems with high throughput and strong consistency guarantees.
/// </summary>
/// <param name="shortener">The URL shortener to use for generating short URLs.</param>
/// <param name="repository">The repository to use for storing URL mappings.</param>
/// <param name="collisionResolutionStrategy">The strategy to use for resolving collisions. Defaults to null strategy.</param>
/// <param name="maxRetries">Maximum number of retry attempts for collision resolution. Default is 3.</param>
/// <param name="baseRetryDelayMs">Base delay in milliseconds for exponential backoff. Default is 10ms.</param>
public sealed class UrlManager(
    IUrlShortener? shortener,
    IUrlRepository? repository,
    ICollisionResolutionStrategy? collisionResolutionStrategy = null,
    int maxRetries = 3,
    int baseRetryDelayMs = 10)
{
    private readonly IUrlShortener _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
    private readonly IUrlRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ICollisionResolutionStrategy _collisionResolutionStrategy = collisionResolutionStrategy ?? new NullCollisionResolutionStrategy();
    private readonly ResiliencePipeline _retryPolicy = RetryPolicyFactory.CreateRetryPolicy(maxRetries, baseRetryDelayMs);

    /// <summary>
    /// Shortens the specified long URL and saves the mapping to the repository using optimistic concurrency control.
    /// This method is optimized for distributed systems with high throughput while maintaining shortening safety.
    ///
    /// Key features:
    /// - Optimistic concurrency control for high throughput
    /// - Exponential backoff with jitter for retry attempts using Polly v8
    /// - Automatic collision resolution using configurable strategy
    /// - Strong consistency guarantees within regions
    /// - Idempotent behavior for the same input URL
    /// - Preserves original URL integrity (no modification).
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <returns>The generated short URL.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="longUrl"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails after all retry attempts.</exception>
    public string ShortenAndSave(string longUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(longUrl);

        // Use Polly for retry logic with collision resolution
        // Track attempt number manually since Polly v8 doesn't expose it directly in the executed method
        var attemptNumber = 0;
        return _retryPolicy.Execute(() => TryShortenAndSaveSingleAttempt(longUrl, attemptNumber++));
    }

    /// <summary>
    /// Attempts to shorten and save a URL using optimistic concurrency control for a single attempt.
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <param name="attempt">The current attempt number (used for collision resolution).</param>
    /// <returns>The generated short URL if successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails and should be retried by Polly.</exception>
    private string TryShortenAndSaveSingleAttempt(string longUrl, int attempt)
    {
        // Generate short URL using collision resolution strategy
        var shortUrl = GenerateUniqueShortUrl(longUrl, attempt);

        // Try optimistic save (expecting no existing mapping)
        var success = _repository.TrySaveUrlMapping(shortUrl, longUrl, expectedValue: null, out var currentValue);

        if (success)
        {
            // Successfully saved new mapping
            return shortUrl;
        }

        // Check if the collision is with the same URL (idempotency)
        if (currentValue is not null && string.Equals(currentValue, longUrl, StringComparison.Ordinal))
        {
            // Idempotent case: same input returns the same slug
            return shortUrl;
        }

        // Collision with different URL - this is expected in distributed systems
        // Throw exception to trigger Polly retry with different unique shortener
        throw new InvalidOperationException($"Collision detected for short URL '{shortUrl}'. Retry with different collision resolution strategy.");
    }

    /// <summary>
    /// Generates a unique short URL using the configured collision resolution strategy.
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <param name="attempt">The current attempt number (used for uniqueness).</param>
    /// <returns>The generated short URL.</returns>
    private string GenerateUniqueShortUrl(string longUrl, int attempt)
    {
        // Generate the base short URL using the primary shortener
        var baseShortUrl = _shortener.ShortenUrl(longUrl);

        // Use the collision resolution strategy to generate a unique short URL
        return _collisionResolutionStrategy.GenerateUniqueShortUrl(baseShortUrl, attempt, longUrl);
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public string GetOriginalUrl(string shortUrl)
    {
        return _repository.GetOriginalUrl(shortUrl) ?? string.Empty;
    }
}
