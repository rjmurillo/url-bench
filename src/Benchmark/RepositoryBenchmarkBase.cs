using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using Logic.Shorteners;
using UrlConverterPermutations;

namespace Benchmark;

public abstract class RepositoryBenchmarkBase
{
    private IUrlShortener _urlShortener = null!;

    [Params(
        // This is the fastest out of all of the shorteners
        typeof(IncrementingIntegerUrlShortener)
    )]
    public Type ShortenerType { get; set; }

    [ParamsSource(nameof(RepositoryTypes))]
    public Type RepositoryType { get; set; }

    public IEnumerable<Type> RepositoryTypes => UrlRepositoryHelper.GetConcreteTypes<IUrlRepository>().Take(1);

    private const string LongUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

    [Params(1, 1_000, 1_000_000)]
    public int NumberOfUrls;

    protected Facade Facade { get; private set; }

    private IUrlRepository _repo = null!;
    protected internal HashSet<string> ShortUrls { get; private set; }

    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType);
        _repo = Factory.CreateRepository(RepositoryType);
        ShortUrls = new HashSet<string>(NumberOfUrls);
        Facade = new Facade(_urlShortener, _repo);

        for (var i = 0; i <= NumberOfUrls; i++)
        {
            ShortUrls.Add(Facade.ShortenAndSave(LongUrl + i));
        }
        
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

        _urlShortener = null;
        _repo = null;
        Facade = null;
    }
}