using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Contracts;
using Logic;
using Logic.Repositories;
using Logic.Shorteners;
using UrlConverterPermutations;
using Convert = Logic.Convert;

var config = DefaultConfig.Instance
        //.With(Job.MediumRun.WithRuntime(CoreRuntime.Core60).WithId("Net60").WithToolchain(InProcessEmitToolchain.Instance).WithStrategy(RunStrategy.Throughput).WithIterationCount(3).AsBaseline())
        .With(Job.MediumRun.WithRuntime(CoreRuntime.Core80).WithId("Net80").WithToolchain(InProcessEmitToolchain.Instance).WithStrategy(RunStrategy.Throughput).WithIterationCount(1))
    //.With(Job.MediumRun.WithRuntime(CoreRuntime.Core80).WithId("Net90").WithToolchain(InProcessEmitToolchain.Instance).WithStrategy(RunStrategy.Throughput).WithIterationCount(3))
    ;

BenchmarkRunner.Run<Benchmarks>(config);

//[RPlotExporter]
[MemoryDiagnoser]
//[DotTraceDiagnoser]
[GcServer(true)]
public class Benchmarks
{
    private IUrlShortener _urlShortener = null!;

    [Params(
        // This is the fastest out of all of the shorteners
        typeof(IncrementingIntegerUrlShortener)
       )]
    public Type ShortenerType;

    [Params(
               typeof(ConcurrentUrlRepository)
               , typeof(FastCacheUrlRepository)
               , typeof(FASTERUrlRepository)
               , typeof(MemoryCacheUrlRepository)
               , typeof(PartitionedUrlRepository)
               , typeof(ReaderWriterLockSlimUrlRepository)
                  )]
    public Type RepositoryType;

    [Params(
        "https://jobs.careers.microsoft.com/us/en/job/1613473"
    )]
    public string LongUrl;

    [Params(1, 100, 1_000_000, 10_000_000)]
    public int NumberOfUrls;

    private Facade _facade;
    private Random _rnd;

    [GlobalSetup]
    public void Setup()
    {
        var urlShortener = Factory.CreateShortener(ShortenerType);
        var repo = Factory.CreateRepository(RepositoryType);

        _facade = new Facade(urlShortener, repo);

        for (var i = 0; i <= NumberOfUrls; i++)
        {
            _facade.ShortenAndSave(LongUrl);
        }

        _rnd = new Random(0);
    }

    [Benchmark]
    public void Retrieve_Random()
    {
        for (var i = 0; i <= NumberOfUrls; i++)
        {
            var k = (ulong)_rnd.Next(1, NumberOfUrls);
            var shortUrl = Convert.AsBase62String(k).ToString();
            var longUrl = _facade.GetOriginalUrl(shortUrl);
        }
    }
}