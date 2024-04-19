using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using UrlConverterPermutations;

namespace Benchmark;

public abstract class ShortenerBenchmarksBase : IShortenerBenchmark
{
    protected IUrlShortener UrlShortener { get; set; } = null!;

    [ParamsSource(nameof(ShortenerTypes))]
    public Type ShortenerType { get; set; }

    public IEnumerable<Type> ShortenerTypes => UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>();
    public string LongUrl => "https://jobs.careers.microsoft.com/us/en/job/1613473";

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