using BenchmarkDotNet.Attributes;
using Proto.Utilities.Benchmark;

using UrlConverterPermutations;

namespace Benchmark;

public class ShortenerParallel : ShortenerBenchmarksBase
{
    // -1 means no limits
    // 1 means single-threaded
    // 2 means two threads
    [Params(-1, 1, 2, 8, 16)]
    public int MaxConcurrency { get; set; }

    private BenchmarkThreadHelper? _threadHelper;
    

    [GlobalSetup(Target = nameof(ShortenParallel))]
    public void SetupParallel()
    {
        base.Setup();

        _threadHelper = new BenchmarkThreadHelper(MaxConcurrency);
        for (var i = 0; i < 100; i++)
        {
            _threadHelper.Add(() => UrlShortener.ShortenUrl(LongUrl));
        }
    }

    [Benchmark]
    public void ShortenParallel()
    {
        _threadHelper!.ExecuteAndWait();
    }

    [GlobalCleanup(Target = nameof(ShortenParallel))]
    public void CleanupParallel()
    {
        _threadHelper?.Dispose();
    }
}