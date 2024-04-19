using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Contracts;
using Logic;
using Logic.Shorteners;
using UrlConverterPermutations;

public class Shortener
{
    private IUrlShortener _urlShortener = null!;

    [ParamsSource(nameof(ShortenerTypes))]
    public Type ShortenerType;

    public IEnumerable<Type> ShortenerTypes => UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>();

    [Params(
        "https://jobs.careers.microsoft.com/us/en/job/1613473"
    )]
    public string LongUrl;

    [GlobalSetup]
    public void Setup()
    {
        _urlShortener = Factory.CreateShortener(ShortenerType);
    }

    [Benchmark]
    public string Shorten()
    {
        return _urlShortener.ShortenUrl(LongUrl);
    }
}