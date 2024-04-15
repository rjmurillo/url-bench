using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.dotTrace;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Contracts;
using Logic.Repositories;
using Logic.Shorteners;

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

    [Params(typeof(ReadOnlyMemoryUrlShortener))]
    public Type ShortenerType;

    //[Params(typeof(ReaderWriterLockSlimUrlRepository), typeof(PartitionedUrlRepository), typeof(ConcurrentUrlRepository), typeof(FASTERUrlRepository))]
    //[Params(typeof(FASTERUrlRepository), typeof(ConcurrentUrlRepository))]
    //[Params(typeof(FASTERUrlRepository))]
    //[Params(typeof(ReaderWriterLockSlimUrlRepository), typeof(ReaderWriterLockSlimReadOnlyMemoryRepository))]
    [Params(typeof(ReaderWriterLockSlimReadOnlyMemoryRepository))]
    public Type RepositoryType;

    [Params("https://jobs.careers.microsoft.com/us/en/job/1613473")]
    public string LongUrl;

    private string _knownShort;

    [GlobalSetup]
    public void Setup()
    {
        var repository = (IUrlRepository)Activator.CreateInstance(RepositoryType);
        _urlShortener = (IUrlShortener)Activator.CreateInstance(ShortenerType, new object[]{repository});

        _knownShort = _urlShortener.ShortenUrl(LongUrl);
    }

    [Benchmark]
    public void Shorten()
    {
        var shortUrl = _urlShortener.ShortenUrl(LongUrl);
    }

    [Benchmark]
    public void Shorten_Memory()
    {
        if (_urlShortener is ReadOnlyMemoryUrlShortenerBase urlShortener)
        {
            var shortUrl = urlShortener.ShortenUrl(LongUrl.AsMemory());
        }
        else
        {
            throw new NotSupportedException($"Type {_urlShortener.GetType().Name} does not support {nameof(ReadOnlyMemory<char>)}.");
        }
        
        
    }

    //[Benchmark]
    //public void RetrieveOriginalUrl()
    //{
    //    _urlShortener.RetrieveOriginalUrl(_knownShort);
    //}

    [Benchmark]
    public void RoundTrip()
    {
        var shortUrl = _urlShortener.ShortenUrl(LongUrl);
        var originalUrl = _urlShortener.RetrieveOriginalUrl(shortUrl);
    }

    [Benchmark]
    public void RoundTrip_Memory()
    {
        if (_urlShortener is ReadOnlyMemoryUrlShortenerBase urlShortener)
        {
            var shortUrl = urlShortener.ShortenUrl(LongUrl.AsMemory());
            var originalUrl = urlShortener.RetrieveOriginalUrl(shortUrl.ToArray());
        }
        else
        {
            throw new NotSupportedException($"Type {_urlShortener.GetType().Name} does not support {nameof(ReadOnlyMemory<char>)}.");
        }
    }
}