using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using Proto.Utilities.Benchmark;
using UrlConverterPermutations;

namespace Benchmark;

public interface IShortenerBenchmark
{
    public Type ShortenerType { get; set; }
}

public class Shortener : ShortenerBenchmarksBase
{
    

    [Benchmark]
    public string Shorten()
    {
        return UrlShortener.ShortenUrl(LongUrl);
    }

    
}