using Benchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
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
using Convert = Logic.Convert;

public interface IRepositoryBenchmark
{
    Type RepositoryType { get; set; }
}

public class Repository : IRepositoryBenchmark, IShortenerBenchmark
{
    private IUrlShortener _urlShortener = null!;

    [Params(
        // This is the fastest out of all of the shorteners
        typeof(IncrementingIntegerUrlShortener)
       )]
    public Type ShortenerType { get; set; }

    [ParamsSource(nameof(RepositoryTypes))]
    public Type RepositoryType { get; set; }

    public IEnumerable<Type> RepositoryTypes => UrlRepositoryHelper.GetConcreteTypes<IUrlRepository>();

    private const string LongUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

    [Params(1, 1_000, 1_000_000)]
    public int NumberOfUrls;

    private Facade _facade;
    private Random _rnd;
    private IUrlRepository _repo = null!;
    private HashSet<string> _shortUrls;

    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType);
        _repo = Factory.CreateRepository(RepositoryType);
        _shortUrls = new HashSet<string>(NumberOfUrls);
        _facade = new Facade(_urlShortener, _repo);

        for (var i = 0; i <= NumberOfUrls; i++)
        {
            _shortUrls.Add(_facade.ShortenAndSave(LongUrl + i));
        }
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
        var shortUrl = _shortUrls.ElementAt(i);
        return _facade.GetOriginalUrl(shortUrl);
    }
}