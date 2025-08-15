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
public class Repository
{
    private IUrlShortener? _urlShortener;

    [Params(
        // This is the fastest out of all of the shorteners
        typeof(IncrementingIntegerUrlShortener)
       )]
    public Type? ShortenerType;

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

    [Params(1, 1_000, 1_000_000)]
    public int NumberOfUrls;

    private Facade? _facade;
    private Random? _rnd;
    private IUrlRepository? _repo = null!;
    private HashSet<string>? _shortUrls;

    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType);
        _repo = Factory.CreateRepository(RepositoryType);
        _shortUrls = new HashSet<string>(NumberOfUrls);
        _facade = new Facade(_urlShortener, _repo);

        for (var i = 0; i <= NumberOfUrls; i++)
        {
            _shortUrls.Add(_facade.ShortenAndSave(LongUrl + i));
        }
        _rnd = new Random(0);
    }

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

    [Benchmark]
    public string Retrieve_Random()
    {
        var i = _rnd.Next(0, NumberOfUrls);
        var shortUrl = _shortUrls.ElementAt(i);
        return _facade.GetOriginalUrl(shortUrl);
    }
}