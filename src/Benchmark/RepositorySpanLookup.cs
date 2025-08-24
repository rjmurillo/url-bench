using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Logic.Contracts;
using Logic.Repositories;

namespace Benchmark;

[InliningDiagnoser(true, new[] { "Logic.Repositories" })]
[MemoryDiagnoser]
[JitStatsDiagnoser]
[GcServer(true)]
[SimpleJob(RunStrategy.Throughput)]
public class RepositorySpanLookup
{
    private ConcurrentUrlRepository? _repo;
    private IUrlRepository? _iface;

    private string _shortUrl = null!;
    private string _originalUrl = null!;

    // Simulate hot-path buffer that holds the short code (like BenchmarkCommand.ReadSingle)
    private readonly char[] _hotPathBuffer = new char[12];

    [GlobalSetup]
    public void Setup()
    {
        _repo = new ConcurrentUrlRepository();
        _iface = _repo;

        _originalUrl = "https://contoso.example/path/resource";
        _shortUrl = "AbCdEf123"; // arbitrary short token under 12 chars

        // Seed repository
        _repo.SaveUrlMapping(_shortUrl, _originalUrl);

        // Seed the hot-path buffer with the same content
        _shortUrl.AsSpan().CopyTo(_hotPathBuffer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _repo?.Dispose();
        _repo = null;
        _iface = null;
    }

    // 1) Baseline: slice.ToString() then use legacy interface
    // This intentionally allocates for the string materialization.
    [Benchmark(Baseline = true)]
    public string Baseline_ToString_Then_Interface()
    {
        var span = _hotPathBuffer.AsSpan(0, _shortUrl.Length);
        string key = span.ToString(); // allocation
        return _iface!.GetOriginalUrl(key);
    }

    // 2) Extension method on interface using ReadOnlySpan<char>
    // Expected: 0 allocations on .NET 9 when repository supports IUrlRepositorySpan
    // (ConcurrentUrlRepository does). On .NET 8, it will fall back and allocate once.
    [Benchmark]
    public string Extension_Span_Interface()
    {
        var span = _hotPathBuffer.AsSpan(0, _shortUrl.Length);
        return _iface!.GetOriginalUrl(span);
    }

    // 3) Hot-path stackalloc variant matching ReadSingle pattern
    [Benchmark]
    public string Extension_Span_Stackalloc()
    {
        Span<char> buf = stackalloc char[12];
        var src = _shortUrl.AsSpan();
        src.CopyTo(buf);
        var slice = buf[..src.Length];
        return _iface!.GetOriginalUrl(slice);
    }
}
