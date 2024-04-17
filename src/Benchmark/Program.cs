using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.dotTrace;
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
    private IUrlShortener _urlShortener;

    [Params(
        typeof(IncrementingIntegerUrlShortener)
        , typeof(SnowflakeUrlShortener)
#if NET8_0_OR_GREATER
        ,typeof(Sha256HashUrlShortener)
        ,typeof(Murmur3HashUrlShortener)
#endif
    )]
    public Type ShortenerType;

    //[Params(typeof(ReaderWriterLockSlimUrlRepository), typeof(PartitionedUrlRepository), typeof(ConcurrentUrlRepository), typeof(FASTERUrlRepository))]
    //[Params(typeof(FASTERUrlRepository), typeof(ConcurrentUrlRepository))]
    //[Params(typeof(FASTERUrlRepository))]
    //[Params(typeof(ReaderWriterLockSlimUrlRepository), typeof(ReaderWriterLockSlimReadOnlyMemoryRepository))]
    [Params(typeof(FASTERUrlRepository))]
    public Type RepositoryType;

    [Params("https://jobs.careers.microsoft.com/us/en/job/1613473")]
    public string LongUrl;

    private string _knownShort;
    private IUrlRepository? _repository;
    private Facade _facade;

    [GlobalSetup]
    public void Setup()
    {
        _repository = Factory.CreateRepository(RepositoryType);
        _urlShortener = Factory.CreateShortener(ShortenerType);
        _facade = new Facade(_urlShortener, _repository);

        _knownShort = _urlShortener.ShortenUrl(LongUrl);
        _repository.SaveUrlMapping(_knownShort, LongUrl);
    }

    [Benchmark]
    public void Shorten()
    {
        var shortUrl = _urlShortener.ShortenUrl(LongUrl);
    }

    [Benchmark]
    public void RetrieveOriginalUrl()
    {
        _repository.GetOriginalUrl(_knownShort);
    }

    [Benchmark]
    public void RoundTrip()
    {
        var shortUrl = _facade.ShortenAndSave(LongUrl);
        var originalUrl = _facade.GetOriginalUrl(shortUrl);
    }
}