using Contracts;

namespace Logic;

/// <summary>
/// Provides a simplified interface for URL shortening operations by combining a URL shortener and repository.
/// Optimized for distributed systems with high throughput and strong consistency guarantees.
/// </summary>
/// <param name="shortener">The URL shortener to use for generating short URLs.</param>
/// <param name="repository">The repository to use for storing URL mappings.</param>
/// <param name="maxRetries">Maximum number of retry attempts for collision resolution. Default is 3.</param>
/// <param name="baseRetryDelayMs">Base delay in milliseconds for exponential backoff. Default is 10ms.</param>
public sealed class UrlManager(IUrlShortener? shortener, IUrlRepository? repository, int maxRetries = 3, int baseRetryDelayMs = 10)
{
    private readonly IUrlShortener _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
    private readonly IUrlRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly int _maxRetries = maxRetries > 0 ? maxRetries : throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be positive.");
    private readonly int _baseRetryDelayMs = baseRetryDelayMs > 0 ? baseRetryDelayMs : throw new ArgumentOutOfRangeException(nameof(baseRetryDelayMs), "Base retry delay must be positive.");
    private readonly Random _random = new();

    /// <summary>
    /// Shortens the specified long URL and saves the mapping to the repository using optimistic concurrency control.
    /// This method is optimized for distributed systems with high throughput while maintaining shortening safety.
    ///
    /// Key features:
    /// - Optimistic concurrency control for high throughput
    /// - Exponential backoff with jitter for retry attempts
    /// - Automatic collision resolution using unique shortener
    /// - Strong consistency guarantees within regions
    /// - Idempotent behavior for the same input URL
    /// - Preserves original URL integrity (no modification).
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <returns>The generated short URL.</returns>
    /// <exception cref="InvalidOperationException">Thrown when all retry attempts are exhausted or the repository is unavailable.</exception>
    public string ShortenAndSave(string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
        {
            return string.Empty;
        }

        // Try optimistic save with exponential backoff and collision resolution
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var result = TryShortenAndSaveOptimistic(longUrl, attempt);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            catch (Exception) when (attempt < _maxRetries)
            {
                // Continue to retry on transient failures
            }

            // Exponential backoff with jitter (except on last attempt)
            if (attempt < _maxRetries)
            {
                var delayMs = (_baseRetryDelayMs * (1 << attempt)) + _random.Next(_baseRetryDelayMs);
                Thread.Sleep(delayMs);
            }
        }

        throw new InvalidOperationException($"Failed to shorten URL after {_maxRetries + 1} attempts. Repository may be unavailable or experiencing high contention.");
    }

    /// <summary>
    /// Attempts to shorten and save a URL using optimistic concurrency control.
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <param name="attempt">The current attempt number (used for unique shortener).</param>
    /// <returns>The generated short URL if successful; otherwise, empty string.</returns>
    private string TryShortenAndSaveOptimistic(string longUrl, int attempt)
    {
        // Generate short URL using unique shortener for collision resolution
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
        // Return empty string to trigger retry with different unique shortener
        return string.Empty;
    }

    /// <summary>
    /// Generates a unique short URL for collision resolution without altering the original URL.
    /// </summary>
    /// <param name="longUrl">The long URL to shorten.</param>
    /// <param name="attempt">The current attempt number (used for uniqueness).</param>
    /// <returns>The generated short URL.</returns>
    private string GenerateUniqueShortUrl(string longUrl, int attempt)
    {
        // Use the base shortener for attempt 0 (deterministic)
        if (attempt == 0)
        {
            return _shortener.ShortenUrl(longUrl);
        }

        // For retry attempts, use a unique shortener that doesn't modify the URL
        // This could be a distributed counter, UUID-based shortener, or timestamp-based approach
        // For now, we'll use a simple approach with attempt number as a suffix
        var baseShortUrl = _shortener.ShortenUrl(longUrl);

        // Append a unique suffix to avoid collisions while preserving the base shortener's output
        // This maintains the relationship to the original URL while ensuring uniqueness
        return $"{baseShortUrl}{attempt}";
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
