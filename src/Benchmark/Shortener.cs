using BenchmarkDotNet.Attributes;
using Contracts;
using Logic;
using Proto.Utilities.Benchmark;
using UrlConverterPermutations;

namespace Benchmark;

public class Shortener : ShortenerBenchmarks
{
    

    [Benchmark]
    public string Shorten()
    {
        return UrlShortener.ShortenUrl(LongUrl);
    }

    
}