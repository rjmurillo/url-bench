using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Contracts;
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

    [Params("https://jobs.careers.microsoft.com/us/en/job/1613473")]
    public string LongUrl;

    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType);
    }

    [Benchmark]
    public void Shorten()
    {
        var shortUrl = _urlShortener.ShortenUrl(LongUrl);
    }
}