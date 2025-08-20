using Contracts;
using FastCache;

namespace Logic.Repositories;

/// <summary>
/// A URL repository implementation using FastCache for storing URL mappings with expiration support.
/// </summary>
public class FastCacheUrlRepository : IUrlRepository, IDisposable
{
    // FastCache uses a global static store per T. To avoid cross-test/process collisions
    // and ensure repository instance isolation, namespace all keys with a unique prefix.
    private readonly string _ns = Guid.NewGuid().ToString("N") + ":";

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public string GetOriginalUrl(string shortUrl)
    {
        if (Cached<string>.TryGet(_ns + shortUrl, out var originalUrl))
        {
            return originalUrl.Value;
        }

        return string.Empty;
    }

    /// <summary>
    /// Saves a mapping between a short URL and its original URL with no expiration.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>Always returns true as the operation is assumed to succeed.</returns>
    public bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        Cached<string>.Save(_ns + shortUrl, originalUrl, TimeSpan.MaxValue);
    return true;
    }

    /// <summary>
    /// Attempts to save a URL mapping using optimistic concurrency control.
    /// Note: FastCache doesn't support conditional operations, so this is a simplified implementation.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <param name="expectedValue">The expected current value. If null, the key should not exist.</param>
    /// <param name="currentValue">When this method returns, contains the current value if the save failed due to a condition mismatch; otherwise, null.</param>
    /// <returns>true if the save operation was successful; otherwise, false.</returns>
    public bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue)
    {
        var key = _ns + shortUrl;

        // FastCache doesn't support conditional operations, so we implement a simplified version
        if (expectedValue is null)
        {
            // Check if key already exists
            if (Cached<string>.TryGet(key, out var existing))
            {
                currentValue = existing.Value;
                return false;
            }

            // Key doesn't exist, save it
            var retVal = SaveUrlMapping(shortUrl, originalUrl);
            currentValue = null;
            return retVal;
        }

        // Check if current value matches expected
        if (Cached<string>.TryGet(key, out var current))
        {
            if (string.Equals(current.Value, expectedValue, StringComparison.Ordinal))
            {
                // Update the value
                Cached<string>.Save(key, originalUrl, TimeSpan.MaxValue);
                currentValue = null;
                return true;
            }
            else
            {
                // Value doesn't match expected
                currentValue = current.Value;
                return false;
            }
        }

        // Key doesn't exist but we expected it to
        currentValue = null;
        return false;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the FastCacheUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the FastCacheUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        // No-op: FastCache has a global static store; we namespace keys per instance and do not own any disposable resources.
        _ = disposing;
    }
}
