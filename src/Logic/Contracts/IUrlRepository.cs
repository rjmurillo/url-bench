namespace Contracts;

/// <summary>
/// Defines a repository for storing and retrieving URL mappings between short URLs and their original URLs.
/// </summary>
public interface IUrlRepository
{
    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, null.</returns>
    string GetOriginalUrl(string shortUrl);

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully saved; otherwise, false.</returns>
    bool SaveUrlMapping(string shortUrl, string originalUrl);

    /// <summary>
    /// Attempts to save a URL mapping using optimistic concurrency control.
    /// This method is designed for distributed systems where race conditions may occur.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <param name="expectedValue">The expected current value. If null, the key should not exist.</param>
    /// <param name="currentValue">When this method returns, contains the current value if the save failed due to a condition mismatch; otherwise, null.</param>
    /// <returns>true if the save operation was successful; otherwise, false.</returns>
    bool TrySaveUrlMapping(string shortUrl, string? originalUrl, string? expectedValue, out string? currentValue);
}
