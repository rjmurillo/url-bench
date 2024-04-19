using Benchmark;
using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using Logic.Shorteners;
using UrlConverterPermutations;

public interface IRepositoryBenchmark
{
    Type RepositoryType { get; set; }
}

public class Repository : RepositoryBenchmarkBase, IRepositoryBenchmark, IShortenerBenchmark
{
    private Random _rnd;

    [GlobalSetup(Target = nameof(Retrieve_Random))]
    public void Random_Setup()
    {
        base.Setup();
        
        // We want it to be random, but consistent run to run
        _rnd = new Random(0);
    }

    [Benchmark]
    public string Retrieve_Random()
    {
        var i = _rnd.Next(0, NumberOfUrls);
        var shortUrl = ShortUrls.ElementAt(i);
        return Facade.GetOriginalUrl(shortUrl);
    }
}