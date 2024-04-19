using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using Proto.Utilities.Benchmark;

using UrlConverterPermutations;

namespace Benchmark;

public abstract class ShortenerBenchmarks
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

public class ShortenerParallel : ShortenerBenchmarks
{
    [Params(-1, 1, 2)]
    public int MaxConcurrency { get; set; }

    private BenchmarkThreadHelper? threadHelper;
    private ParallelOptions? parallelOptions;
    private Action[] parallelActions;

    [GlobalSetup(Target = nameof(ShortenParallel))]
    public void SetupParallel()
    {
        UrlShortener = Factory.CreateShortener(ShortenerType);

        //parallelOptions = new ParallelOptions
        //{
        //    MaxDegreeOfParallelism = MaxConcurrency
        //};

        //parallelActions = new Action[100];

        //for (var i = 0; i < 100; i++)
        //{
        //    parallelActions[i] = () => UrlShortener.ShortenUrl(LongUrl);
        //}

        threadHelper = new BenchmarkThreadHelper(MaxConcurrency)
        {
            () => UrlShortener.ShortenUrl(LongUrl)
        };
    }

    [Benchmark]
    public void ShortenParallel()
    {
        //Parallel.Invoke(parallelOptions, parallelActions);
        threadHelper!.ExecuteAndWait();
    }

    [GlobalCleanup(Target = nameof(ShortenParallel))]
    public void CleanupParallel()
    {
        threadHelper?.Dispose();
    }
}