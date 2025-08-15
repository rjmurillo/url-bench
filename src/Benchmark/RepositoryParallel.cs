using Benchmark;
using BenchmarkDotNet.Attributes;
using Proto.Utilities.Benchmark;

public class RepositoryParallel : RepositoryBenchmarkBase
{
    // -1 means no limits
    // 1 means single-threaded
    // 2 means two threads
    [Params(-1, 1, 2, 8, 16)]
    public int MaxConcurrency { get; set; }

    private BenchmarkThreadHelper? _threadHelper;
    private Random _rnd;

    [GlobalSetup(Target = nameof(RetrieveParallel))]
    public void SetupParallel()
    {
        base.Setup();

        // We want it to be random, but consistent run to run
        _rnd = new Random(0);

        _threadHelper = new BenchmarkThreadHelper(MaxConcurrency);
        for (var i = 0; i < 100; i++)
        {
            _threadHelper.Add(() =>
            {
                var idx = _rnd.Next(0, NumberOfUrls);
                var shortUrl = ShortUrls.ElementAt(idx);
                var longUrl = Facade.GetOriginalUrl(shortUrl);
            });
        }
    }

    [Benchmark]
    public void RetrieveParallel()
    {
        _threadHelper!.ExecuteAndWait();
    }

    [GlobalCleanup(Target = nameof(RetrieveParallel))]
    public void CleanupParallel()
    {
        _threadHelper?.Dispose();
    }
}