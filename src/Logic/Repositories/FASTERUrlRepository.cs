using FASTER.core;

namespace Logic.Repositories;

// We're using this because
// 1. it can handle concurrency,
// 2. stores more data than fits into memory,
// 3. can be persisted to disk
#pragma warning disable S101 // Types should be named in PascalCase
/// <summary>
/// A URL repository implementation using FASTER for high-performance concurrent key-value storage.
/// </summary>
public class FASTERUrlRepository : UrlRepositoryBase, IDisposable
#pragma warning restore S101 // Types should be named in PascalCase
{
    private readonly FasterKVSettings<string, string> _settings;
    private readonly FasterKV<string, string> _store;
    private readonly ClientSession<string, string, string, string, Empty, SimpleFunctions<string, string>> _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="FASTERUrlRepository"/> class with default settings.
    /// </summary>
    public FASTERUrlRepository()
    {
        _settings = new FasterKVSettings<string, string>(null);
        _store = new FasterKV<string, string>(_settings);
        _session = _store.For(new SimpleFunctions<string, string>()).NewSession<SimpleFunctions<string, string>>();
    }

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public override string GetOriginalUrl(string shortUrl)
    {
        var retVal = string.Empty;
        var status = _session.Read(ref shortUrl, ref retVal);

        return retVal;
    }

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the operation completed successfully; otherwise, false.</returns>
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        var status = _session.Upsert(ref shortUrl, ref originalUrl);
        return status.IsCompletedSuccessfully;
    }

    /// <summary>
    /// Attempts to save a URL mapping using optimistic concurrency control.
    /// Note: FASTER doesn't support conditional operations, so this is a simplified implementation.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <param name="expectedValue">The expected current value. If null, the key should not exist.</param>
    /// <param name="currentValue">When this method returns, contains the current value if the save failed due to a condition mismatch; otherwise, null.</param>
    /// <returns>true if the save operation was successful; otherwise, false.</returns>
    public override bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue)
    {
        // FASTER doesn't support conditional operations, so we implement a simplified version
        if (expectedValue is null)
        {
            // Check if key already exists
            var existingValue = string.Empty;
            var readStatus = _session.Read(ref shortUrl, ref existingValue);

            if (readStatus.IsCompletedSuccessfully && !string.IsNullOrEmpty(existingValue))
            {
                currentValue = existingValue;
                return false;
            }

            // Key doesn't exist, save it
            var status = _session.Upsert(ref shortUrl, ref originalUrl);
            currentValue = null;
            return status.IsCompletedSuccessfully;
        }

        // Check if current value matches expected
        var current = string.Empty;
        var readResult = _session.Read(ref shortUrl, ref current);

        if (readResult.IsCompletedSuccessfully && !string.IsNullOrEmpty(current))
        {
            if (string.Equals(current, expectedValue, StringComparison.Ordinal))
            {
                // Update the value
                var status = _session.Upsert(ref shortUrl, ref originalUrl);
                currentValue = null;
                return status.IsCompletedSuccessfully;
            }
            else
            {
                // Value doesn't match expected
                currentValue = current;
                return false;
            }
        }
        else
        {
            // Key doesn't exist but we expected it to
            currentValue = null;
            return false;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the FASTERUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the FASTERUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            _session.Dispose();
            _settings.Dispose();
            _store.Dispose();
        }
    }
}
