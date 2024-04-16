using Contracts;
using FASTER.core;

namespace Logic.Repositories;

// We're using this because
// 1. it can handle concurrency,
// 2. stores more data than fits into memory,
// 3. can be persisted to disk
#pragma warning disable S101 // Types should be named in PascalCase
public class FASTERUrlRepository : ReadOnlyMemoryUrlRepositoryBase, IDisposable
#pragma warning restore S101 // Types should be named in PascalCase
{
    private readonly FasterKVSettings<string, string> _settings;
    private readonly FasterKV<string, string> _store;
    private readonly ClientSession<string, string, string, string, Empty, SimpleFunctions<string, string>> _session;

    public FASTERUrlRepository()
    {
        _settings = new FasterKVSettings<string, string>(null);
        _store = new FasterKV< string, string>(_settings);
        _session = _store.For(new SimpleFunctions<string, string>()).NewSession<SimpleFunctions<string, string>>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            _session.Dispose();
            _settings.Dispose();
            _store.Dispose();
        }

        // Dispose unmanaged resources
    }

    public override string GetOriginalUrl(ReadOnlyMemory<char> shortUrl)
    {
        var shortu = shortUrl.ToString();
        var retVal = string.Empty;
        var status = _session.Read(ref shortu, ref retVal);

        return retVal;
    }

    public override bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, string originalUrl)
    {
        var shortu = shortUrl.ToString();
        var status = _session.Upsert(ref shortu, ref originalUrl);
        return status.IsCompletedSuccessfully;
    }
}