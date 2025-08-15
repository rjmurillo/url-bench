using Benchmark;
using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using Logic.Shorteners;
using Convert = Logic.Convert;

public interface IRepositoryBenchmark
{
    private IUrlShortener? _urlShortener;

public class Repository : RepositoryBenchmarkBase, IRepositoryBenchmark, IShortenerBenchmark
{
    private Random _rnd;
    private IUrlRepository? _repo = null!;
    private HashSet<string> _shortUrls;

    [GlobalSetup(Target = nameof(Retrieve_Random))]
    public void Random_Setup()
    {
        base.Setup();
        
        // We want it to be random, but consistent run to run
        _rnd = new Random(0);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_urlShortener is IDisposable ud)
        {
            ((IDisposable)ud).Dispose();
        }

        _urlShortener = null;

        if (_repo is IDisposable rd)
        {
            ((IDisposable)rd).Dispose();
        }

        _repo = null;
        _facade = null;
    }

    [Benchmark]
    public string Retrieve_Random()
    {
        var i = _rnd.Next(0, NumberOfUrls);
        var shortUrl = ShortUrls.ElementAt(i);
        return Facade.GetOriginalUrl(shortUrl);
    }
}