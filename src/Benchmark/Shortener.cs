using BenchmarkDotNet.Attributes;

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