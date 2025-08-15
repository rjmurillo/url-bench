using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Contracts;
using Logic;
using Logic.Shorteners;

[InliningDiagnoser(true, new[] { "Logic.Shorteners", "Logic.Repositories" })]
// GC and Memory allocation
[MemoryDiagnoser]
//[DotTraceDiagnoser]
// Profile the code
//[EtwProfiler]
// Get details about what the JIT is doing
[JitStatsDiagnoser]
[GcServer(true)]
[SimpleJob(RunStrategy.Throughput)]
public class Shortener
{
    private IUrlShortener? _urlShortener;

    [Params(
        typeof(IncrementingIntegerUrlShortener)
        , typeof(SnowflakeUrlShortener)
        , typeof(Sha256HashUrlShortener)
        , typeof(Murmur3HashUrlShortener)
        , typeof(VectorizedUrlShortener)
    )]
    public Type ShortenerType;

    [Params(
        "https://jobs.careers.microsoft.com/us/en/job/1613473"
    )]
    public string LongUrl;


    [Benchmark]
    public string Shorten()
    {
        return UrlShortener.ShortenUrl(LongUrl);
    }


}