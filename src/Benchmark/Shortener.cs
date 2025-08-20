using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Contracts;
using Logic;
using Logic.Shorteners;

namespace Benchmark;

[InliningDiagnoser(true, new[] { "Logic.Shorteners", "Logic.Repositories" })]

// GC and Memory allocation
[MemoryDiagnoser]

// [DotTraceDiagnoser]
// Profile the code
// [EtwProfiler]
// Get details about what the JIT is doing
[JitStatsDiagnoser]
[GcServer(true)]
[SimpleJob(RunStrategy.Throughput)]
public class Shortener
{
    private IUrlShortener? _urlShortener;

    /// <summary>
    /// Gets or sets the type of URL shortener to benchmark.
    /// </summary>
    [Params(
        typeof(IncrementingIntegerUrlShortener),
        typeof(SnowflakeUrlShortener),
        typeof(Sha256HashUrlShortener),
        typeof(Murmur3HashUrlShortener),
        typeof(VectorizedUrlShortener))]
    public Type? ShortenerType;

    /// <summary>
    /// Gets or sets the long URL to use for benchmarking.
    /// </summary>
    [Params(
        "https://jobs.careers.microsoft.com/us/en/job/1613473")]
    public string? LongUrl;

    /// <summary>
    /// Sets up the benchmark by creating the specified URL shortener instance.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType!);
    }

    /// <summary>
    /// Benchmarks the URL shortening operation.
    /// </summary>
    /// <returns>The shortened URL.</returns>
    [Benchmark]
    public string Shorten()
    {
        return _urlShortener!.ShortenUrl(LongUrl!);
    }
}
