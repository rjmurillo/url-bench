using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Contracts;

using Logic;
using Logic.Repositories;
using Logic.Shorteners;

//[RPlotExporter]
[InliningDiagnoser(true, new[] { "Logic.Shorteners", "Logic.Repositories" })]
// GC and Memory allocation
[MemoryDiagnoser]
//[DotTraceDiagnoser]
// Profile the code
//[EtwProfiler]
// Get details about what the JIT is doing
[JitStatsDiagnoser]
[GcServer(true)]
/// <summary>
/// Benchmark class for testing URL repository performance.
/// </summary>
public class Repository
{
    private IUrlShortener? _urlShortener;

    /// <summary>
    /// Gets or sets the type of URL shortener to use for generating short URLs.
    /// </summary>
    [Params(
        // This is the fastest out of all of the shorteners
        typeof(IncrementingIntegerUrlShortener)
       )]
    public Type? ShortenerType;

    /// <summary>
    /// Gets or sets the type of URL repository to benchmark.
    /// </summary>
    [Params(
               typeof(ConcurrentUrlRepository)
               , typeof(FastCacheUrlRepository)
               , typeof(FASTERUrlRepository)
               , typeof(MemoryCacheUrlRepository)
               , typeof(PartitionedUrlRepository)
               , typeof(ReaderWriterLockSlimUrlRepository)
                  )]
    public Type? RepositoryType;

    private const string LongUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

    /// <summary>
    /// Gets or sets the number of URLs to generate for benchmarking.
    /// </summary>
    [Params(1, 1_000, 1_000_000)]
    public int NumberOfUrls;

    private UrlManager? _facade;
    private Random? _rnd;
    private IUrlRepository? _repo = null!;
    private HashSet<string>? _shortUrls;

    /// <summary>
    /// Sets up the benchmark by creating the specified shortener and repository instances and populating them with test data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType);
        _repo = Factory.CreateRepository(RepositoryType);
        _shortUrls = new HashSet<string>(NumberOfUrls);
        _facade = new UrlManager(_urlShortener, _repo);

        for (var i = 0; i <= NumberOfUrls; i++)
        {
            _shortUrls.Add(_facade.ShortenAndSave(LongUrl + i));
        }
        _rnd = new Random(0);
    }

    /// <summary>
    /// Cleans up resources used by the benchmark.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (_urlShortener is IDisposable ud)
        {
            ((IDisposable)ud).Dispose();
        }

        _urlShortener = null;

        if (_repo is IDisposable rd)
        {
            ((IDisposable)rd).Dispose();
        }

        _repo = null;
        _facade = null;
    }

    /// <summary>
    /// Benchmarks the random URL retrieval operation.
    /// </summary>
    /// <returns>The original URL for a randomly selected short URL.</returns>
    [Benchmark]
    public string Retrieve_Random()
    {
        var i = _rnd.Next(0, NumberOfUrls);
        var shortUrl = _shortUrls.ElementAt(i);
        return _facade.GetOriginalUrl(shortUrl);
    }
}