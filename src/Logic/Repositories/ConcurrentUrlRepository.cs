using System.Collections.Concurrent;
using Contracts;
using Logic.Contracts;

namespace Logic.Repositories;

/// <summary>
/// A thread-safe URL repository implementation using ConcurrentDictionary for storing URL mappings.
/// </summary>
public class ConcurrentUrlRepository : IUrlRepositorySpan, IDisposable
{
    private readonly ConcurrentDictionary<string, string> _urlMappings;
#if NET9_0_OR_GREATER
    private readonly ConcurrentDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _altLookup;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentUrlRepository"/> class with default concurrency level and capacity.
    /// </summary>
    public ConcurrentUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentUrlRepository"/> class.
    /// A thread-safe URL repository implementation using ConcurrentDictionary for storing URL mappings.
    /// </summary>
    /// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
    /// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
    public ConcurrentUrlRepository(int concurrencyLevel, int capacity)
    {
        _urlMappings = new ConcurrentDictionary<string, string>(concurrencyLevel, capacity, StringComparer.Ordinal);
#if NET9_0_OR_GREATER
        _altLookup = _urlMappings.GetAlternateLookup<ReadOnlySpan<char>>();
#endif
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public string GetOriginalUrl(string shortUrl)
    {
        if (_urlMappings.TryGetValue(shortUrl, out var originalUrl))
        {
            return originalUrl;
        }

        return string.Empty;
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL as a <see cref="ReadOnlySpan{Char}"/>.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up, provided as a <see cref="ReadOnlySpan{Char}"/>.</param>
    /// <returns>The original URL as a <see cref="ReadOnlySpan{Char}"/> if found; otherwise, an empty span.</returns>
    public string GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl)
    {
#if NET9_0_OR_GREATER
        if (_altLookup.TryGetValue(shortUrl, out var originalUrl))
        {
            return originalUrl;
        }

        return string.Empty;
#else
        return GetOriginalUrl(shortUrl.ToString());
#endif
    }

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully added; otherwise, false.</returns>
    public bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        return _urlMappings.TryAdd(shortUrl, originalUrl);
    }

    /// <summary>
    /// Attempts to save a URL mapping using optimistic concurrency control.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <param name="expectedValue">The expected current value. If null, the key should not exist.</param>
    /// <param name="currentValue">When this method returns, contains the current value if the save failed due to a condition mismatch; otherwise, null.</param>
    /// <returns>true if the save operation was successful; otherwise, false.</returns>
    public bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue)
    {
        // If expectedValue is null, we expect the key to not exist
        if (expectedValue is null)
        {
            var success = SaveUrlMapping(shortUrl, originalUrl);
            if (success)
            {
                currentValue = null;
                return true;
            }

            currentValue = _urlMappings[shortUrl];
            return false;
        }

        // If expectedValue is provided, we need to check if the current value matches
        if (_urlMappings.TryGetValue(shortUrl, out var existingValue))
        {
            if (string.Equals(existingValue, expectedValue, StringComparison.Ordinal))
            {
                // Current value matches expected, update to new value
                var success = _urlMappings.TryUpdate(shortUrl, originalUrl, expectedValue);
                if (success)
                {
                    currentValue = null;
                    return true;
                }

                currentValue = existingValue;
                return false;
            }
            else
            {
                // Current value doesn't match expected
                currentValue = existingValue;
                return false;
            }
        }
        else
        {
            // Key doesn't exist, but we expected it to exist with expectedValue
            currentValue = null;
            return false;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the ConcurrentUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the ConcurrentUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _urlMappings.Clear();
        }
    }
}
