using Contracts;

namespace Logic.Repositories;

public class PartitionedUrlRepository : ReadOnlyMemoryUrlRepositoryBase
{
    private readonly Dictionary<ReadOnlyMemory<char>, string>[] _partitions;
    private readonly ReaderWriterLockSlim[] _locks;

    public PartitionedUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)     // Default to number of cores
    {
    }

    public PartitionedUrlRepository(int numberOfPartitions, int capacity)
    {
        _partitions = new Dictionary<ReadOnlyMemory<char>, string>[numberOfPartitions];
        _locks = new ReaderWriterLockSlim[numberOfPartitions];
        for (var i = 0; i < numberOfPartitions; i++)
        {
            _partitions[i] = new Dictionary<ReadOnlyMemory<char>, string>(capacity, MemoryStringEqualityComparer.Ordinal);
            _locks[i] = new ReaderWriterLockSlim();
        }
    }

    public override string GetOriginalUrl(ReadOnlyMemory<char> shortUrl)
    {
        var partition = GetPartition(shortUrl);
        var l = _locks[partition];

        l.EnterReadLock();
        try
        {
            if (_partitions[partition].TryGetValue(shortUrl.ToArray(), out var originalUrl))
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

    public override bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, string originalUrl)
    {
        var partition = GetPartition(shortUrl);
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