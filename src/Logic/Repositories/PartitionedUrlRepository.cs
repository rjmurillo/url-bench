using Contracts;

namespace Logic.Repositories;

public class PartitionedUrlRepository : UrlRepositoryBase
{
    private readonly Dictionary<string, string>[] _partitions;
    private readonly ReaderWriterLockSlim[] _locks;

    public PartitionedUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)     // Default to number of cores
    {
    }

    public PartitionedUrlRepository(int numberOfPartitions, int capacity)
    {
        _partitions = new Dictionary<string, string>[numberOfPartitions];
        _locks = new ReaderWriterLockSlim[numberOfPartitions];
        for (var i = 0; i < numberOfPartitions; i++)
        {
            _partitions[i] = new Dictionary<string, string>(capacity, StringComparer.Ordinal);
            _locks[i] = new ReaderWriterLockSlim();
        }
    }

    public override string GetOriginalUrl(string shortUrl)
    {
        var partition = GetPartition(shortUrl.AsMemory());
        var l = _locks[partition];

        l.EnterReadLock();
        try
        {
            if (_partitions[partition].TryGetValue(shortUrl, out var originalUrl))
            {
                return originalUrl;
            }
        }
        finally
        {
            l.ExitReadLock();
        }

        return string.Empty;
    }

    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        var partition = GetPartition(shortUrl.AsMemory());
        var l = _locks[partition];

        l.EnterWriteLock();
        try
        {
            return _partitions[partition].TryAdd(
                shortUrl,
                originalUrl);
        }
        finally
        {
            l.ExitWriteLock();
        }
    }

    private int GetPartition(ReadOnlyMemory<char> key) => Math.Abs(MemoryStringEqualityComparer.Ordinal.GetHashCode(key) % _partitions.Length);
}