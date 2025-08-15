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
    /// Initializes a new instance of the FASTERUrlRepository class with default settings.
    /// </summary>
    public FASTERUrlRepository()
    {
        _settings = new FasterKVSettings<string, string>(null);
        _store = new FasterKV<string, string>(_settings);
        _session = _store.For(new SimpleFunctions<string, string>()).NewSession<SimpleFunctions<string, string>>();
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
}