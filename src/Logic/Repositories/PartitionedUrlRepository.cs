namespace Logic.Repositories;

/// <summary>
/// A thread-safe URL repository implementation using partitioned dictionaries with separate locks for improved concurrency.
/// </summary>
public class PartitionedUrlRepository : UrlRepositoryBase, IDisposable
{
    private readonly Dictionary<string, string>[] _partitions;
    private readonly ReaderWriterLockSlim[] _locks;

    /// <summary>
    /// Initializes a new instance of the PartitionedUrlRepository class with default settings.
    /// </summary>
    public PartitionedUrlRepository()
        : this(Environment.ProcessorCount, short.MaxValue)     // Default to number of cores
    {
    }

    /// <summary>
    /// Initializes a new instance of the PartitionedUrlRepository class with the specified number of partitions and capacity.
    /// </summary>
    /// <param name="numberOfPartitions">The number of partitions to use for improved concurrency.</param>
    /// <param name="capacity">The initial capacity for each partition dictionary.</param>
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

    /// <summary>
    /// Retrieves the original URL associated with the specified short URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
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

    /// <summary>
    /// Saves a mapping between a short URL and its original URL.
    /// </summary>
    /// <param name="shortUrl">The short URL to use as the key.</param>
    /// <param name="originalUrl">The original URL to associate with the short URL.</param>
    /// <returns>true if the mapping was successfully added; otherwise, false.</returns>
    public override bool SaveUrlMapping(string shortUrl, string originalUrl)
    {
        var partition = GetPartition(shortUrl.AsMemory());
        var l = _locks[partition];

        l.EnterWriteLock();
        try
        {
            // Upsert semantics to avoid failing when the key already exists
            _partitions[partition][shortUrl] = originalUrl;
            return true;
        }
        finally
        {
            l.ExitWriteLock();
        }
    }

    private int GetPartition(ReadOnlyMemory<char> key) => Math.Abs(MemoryStringEqualityComparer.Ordinal.GetHashCode(key) % _partitions.Length);

    /// <summary>
    /// Releases the unmanaged resources used by the PartitionedUrlRepository and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var partition in _partitions)
            {
                partition.Clear();
            }

            foreach (var l in _locks)
            {
                l.Dispose();
            }
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the PartitionedUrlRepository and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}