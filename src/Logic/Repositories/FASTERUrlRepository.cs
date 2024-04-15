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
    private readonly FasterKVSettings<ReadOnlyMemory<char>, ReadOnlyMemory<char>> _settings;
    private readonly FasterKV<ReadOnlyMemory<char>, ReadOnlyMemory<char>> _store;
    private readonly ClientSession<ReadOnlyMemory<char>, ReadOnlyMemory<char>, ReadOnlyMemory<char>, ReadOnlyMemory<char>, Empty, SimpleFunctions<ReadOnlyMemory<char>, ReadOnlyMemory<char>>> _session;

    public FASTERUrlRepository()
    {
        _settings = new FasterKVSettings<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(null);
        _store = new FasterKV<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(_settings);
        _session = _store.For(new SimpleFunctions<ReadOnlyMemory<char>, ReadOnlyMemory<char>>()).NewSession<SimpleFunctions<ReadOnlyMemory<char>, ReadOnlyMemory<char>>>();
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

    public override ReadOnlySpan<char> GetOriginalUrl(ReadOnlyMemory<char> shortUrl)
    {
        var retVal = ReadOnlyMemory<char>.Empty;
        var status = _session.Read(ref shortUrl, ref retVal);

        if (!status.Found ||
            retVal.IsEmpty)
        {
            retVal = ReadOnlyMemory<char>.Empty;
        }

        return retVal.Span;
    }

    public override bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, ReadOnlyMemory<char> originalUrl)
    {
        var status = _session.Upsert(ref shortUrl, ref originalUrl);
        return status.IsCompletedSuccessfully;
    }
}