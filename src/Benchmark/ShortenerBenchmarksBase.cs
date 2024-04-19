using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using UrlConverterPermutations;

namespace Benchmark;

public abstract class ShortenerBenchmarksBase
{
    protected IUrlShortener UrlShortener { get; set; } = null!;

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
        UrlShortener = Factory.CreateShortener(ShortenerType);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (UrlShortener is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}