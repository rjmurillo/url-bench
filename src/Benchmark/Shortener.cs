using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Logic;
using Logic.Contracts;
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
    public Type? ShortenerType { get; set; }

    /// <summary>
    /// Gets or sets the long URL to use for benchmarking.
    /// </summary>
    [ParamsSource(nameof(UrlsBySize))]
    public string? LongUrl { get; set; }

    /// <summary>
    /// Provides URLs of varying sizes for benchmarking different payload scenarios.
    /// </summary>
    /// <returns>An enumerable of URLs categorized by size: XSmall (&lt;512B), Small (512B-4KB), Medium (4KB-16KB), and Large (&gt;16KB).</returns>
    public static IEnumerable<string> UrlsBySize()
    {
        // XSmall: < 512 bytes
        yield return "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";

        // Small: 512B - 4KB (approximately 1KB)
        var smallQuery = string.Join("&", Enumerable.Repeat("param=value123456789", 40));
        yield return $"https://www.example.com/api/v1/resource/action?{smallQuery}";

        // Medium: 4KB - 16KB (approximately 8KB)
        var mediumQuery = string.Join("&", Enumerable.Repeat("parameter=verylongvaluewithlotsofdatahere123456789", 150));
        yield return $"https://www.example.com/api/v2/complex/resource/with/deep/path/structure?{mediumQuery}";

        // Large: > 16KB (approximately 32KB)
        var largeQuery = string.Join("&", Enumerable.Repeat("largeParameter=extremelylongvaluewithenormousamountsofdatatocreateaverylargeurl123456789012345678901234567890", 300));
        yield return $"https://www.example.com/api/v3/enterprise/complex/resource/with/very/deep/path/structure/for/large/scale/operations?{largeQuery}";
    }

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
