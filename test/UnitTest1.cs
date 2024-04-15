using Contracts;
using Logic;
using Logic.Repositories;
using Logic.Shorteners;
using Shouldly;
using Xunit.Abstractions;

namespace UrlConverterPermutations;

public abstract class ShortenerTests
{
    private readonly ITestOutputHelper _output;
    public IUrlRepository Repository { get; protected set; }
    public IUrlShortener Shortener { get; protected set; }

    protected ShortenerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void OriginalUrl_is_null()
    {
        var shortUrl = Shortener.ShortenUrl(string.Empty);
        shortUrl.ShouldNotBeNull();
        shortUrl.ShouldBeEmpty();
    }

    [Fact]
    public void ShortUrl_is_not_NullOrEmpty()
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl = Shortener.ShortenUrl(longUrl);

        _output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ShortUrl_is_not_same()
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl1 = Shortener.ShortenUrl(longUrl);
        var shortUrl2 = Shortener.ShortenUrl(longUrl);

        _output.WriteLine($"ShortUrl1: {shortUrl1}");
        _output.WriteLine($"ShortUrl2: {shortUrl2}");

        shortUrl1.ShouldNotBe(shortUrl2);
    }

    [Fact(Skip = "Long Running")]
    [Trait("Category", "LongRunning")]
    public void ShortUrl_is_not_same_after_10_million()
    {
        const ulong upper = 10_000_000UL;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var hs = new HashSet<ReadOnlyMemory<char>>(MemoryStringEqualityComparer.Ordinal);

        for (var i = 0UL; i < upper; i+=2UL)
        {
            var a = Shortener.ShortenUrl(longUrl).ToArray();
            var b = Shortener.ShortenUrl(longUrl).ToArray();

            hs.Add(a).ShouldBeTrue($"Duplicate detected: {a}");
            hs.Add(b).ShouldBeTrue($"Duplicate detected: {b}");

            a.ShouldNotBe(b);

            a.ToString().ShouldNotBeNullOrWhiteSpace();
            b.ToString().ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void ShortUrl_thread_safety()
    {
        const int upper = 100;
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var processorCount = Environment.ProcessorCount; // Number of logical CPUs

        Parallel.For(0, upper / 2, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, i =>
        {
            var a = Shortener.ShortenUrl(longUrl);
            var b = Shortener.ShortenUrl(longUrl);

            a.ShouldNotBe(b);

            a.ShouldNotBeNullOrWhiteSpace();
            b.ShouldNotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void ShortUrl_length_less_than_7()
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl = Shortener.ShortenUrl(longUrl);

        _output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.Length.ShouldBeLessThan(8);
    }

    [Fact]
    public void RetrieveOriginalUrl_returns_original_url()
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl = Shortener.ShortenUrl(longUrl);

        _output.WriteLine($"Shortened URL: {shortUrl}");

        var originalUrl = Shortener.RetrieveOriginalUrl(shortUrl);

        _output.WriteLine($"Original URL: {originalUrl}");

        originalUrl.ShouldBe(longUrl);
    }

    [SkippableFact]
    public void MemoryOptimized_RetrieveOriginalUrl_returns_original_url()
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortener = Shortener as ReadOnlyMemoryUrlShortenerBase;

        Skip.If(shortener == null);

        var shortUrl = shortener.ShortenUrl(longUrl.AsMemory());
        _output.WriteLine($"Shortened URL: {shortUrl}");

        var originalUrl = shortener.RetrieveOriginalUrl(new ReadOnlyMemory<char>(shortUrl.ToArray()));

        _output.WriteLine($"Original URL: {originalUrl}");

        originalUrl.ToArray().ShouldBe(longUrl.ToCharArray());
    }

    [Fact]
    public void RetrieveOriginalUrl_returns_result_within_requirements_when_keyspace_is_used()
    {
        var upper = (ulong)(Math.Pow(62, 7) - 1.0d);
        Shortener = new UrlShortener(Repository, upper);

        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";
        var shortUrl = Shortener.ShortenUrl(longUrl);

        _output.WriteLine($"Shortened URL: {shortUrl}");

        var originalUrl = Shortener.RetrieveOriginalUrl(shortUrl);

        _output.WriteLine($"Original URL: {originalUrl}");

        originalUrl.ShouldBe(longUrl);
        shortUrl.Length.ShouldBeLessThan(8);
    }

    [Fact]
    public void TestRoundTrip()
    {
        const string longUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

        var shortUrl = Shortener.ShortenUrl(longUrl) ;

        _output.WriteLine($"Shortened URL: {shortUrl}");

        Assert.True(shortUrl.Length < longUrl.Length, "The short version is longer than the original");

        var originalUrl = Shortener.RetrieveOriginalUrl(shortUrl);

        _output.WriteLine($"Original URL: {originalUrl}");

        Assert.Equal(longUrl, originalUrl);
    }
}

public class UrlShortener_ReaderWriterLockSlimUrlRepository : ShortenerTests
{
    public UrlShortener_ReaderWriterLockSlimUrlRepository(ITestOutputHelper output)
        :base(output)
    {
        Repository = new ReaderWriterLockSlimUrlRepository();
        Shortener = new UrlShortener(Repository);
    }
}

public class UrlShortener_PartitionedUrlRepository : ShortenerTests
{
    public UrlShortener_PartitionedUrlRepository(ITestOutputHelper output)
     : base(output)
    {
        Repository = new PartitionedUrlRepository();
        Shortener = new UrlShortener(Repository);
    }
}

//public class UrlShortener_MemoryCacheUrlRepository : ShortenerTests
//{
//    public UrlShortener_MemoryCacheUrlRepository(ITestOutputHelper output) : base(output)
//    {
//        Repository = new MemoryCacheUrlRepository();
//        Shortener = new UrlShortener(Repository);
//    }
//}

public class UrlShortener_FastCacheUrlRepository : ShortenerTests
{
    public UrlShortener_FastCacheUrlRepository(ITestOutputHelper output) : base(output)
    {
        Repository = new FastCacheUrlRepository();
        Shortener = new UrlShortener(Repository);
    }
}

public sealed class UrlShortener_FasterUrlRepository : ShortenerTests, IDisposable
{
    public UrlShortener_FasterUrlRepository(ITestOutputHelper output) : base(output)
    {
        Repository = new FASTERUrlRepository();
        Shortener = new UrlShortener(Repository);
    }

    public void Dispose()
    {
        var r = Repository as FASTERUrlRepository;
        r?.Dispose();
    }
}

public class Performance_FunctionalTests
{
    private readonly ITestOutputHelper _output;
    private const string LongUrl = "https://jobs.careers.microsoft.com/us/en/job/1613473";

    public Performance_FunctionalTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableTheory]
    [InlineData(typeof(ReaderWriterLockSlimUrlRepository), typeof(UrlShortener))]
    [InlineData(typeof(ReaderWriterLockSlimReadOnlyMemoryRepository), typeof(ReadOnlyMemoryUrlShortener))]
    public void StringTests(Type repositoryType, Type shortenerType)
    {
        // Arrange
        var shortener = TypeFactory(repositoryType, shortenerType);

        // Regular
        var shortUrl = shortener.ShortenUrl(LongUrl);
        _output.WriteLine($"Shortened URL: {shortUrl}");

        shortUrl.ShouldNotBeNullOrEmpty();
        shortUrl.ShouldNotBeNullOrWhiteSpace();
        shortUrl.Length.ShouldBeLessThan(8);

        var retrievedOriginalUrl = shortener.RetrieveOriginalUrl(shortUrl);
        _output.WriteLine($"Retrieved URL: {retrievedOriginalUrl}");

        retrievedOriginalUrl.ShouldNotBeNullOrEmpty();
        retrievedOriginalUrl.ShouldNotBeNullOrWhiteSpace();
        retrievedOriginalUrl.ShouldBe(LongUrl);
    }

    [SkippableTheory]
    [InlineData(typeof(ReaderWriterLockSlimUrlRepository), typeof(UrlShortener))]
    [InlineData(typeof(ReaderWriterLockSlimReadOnlyMemoryRepository), typeof(ReadOnlyMemoryUrlShortener))]
    public void MemoryTests(Type repositoryType, Type shortenerType)
    {
        // Arrange
        var shortener = TypeFactory(repositoryType, shortenerType);

        // Memory
        if (shortener is ReadOnlyMemoryUrlShortenerBase urlShortener)
        {
            var shortUrlMemory = urlShortener.ShortenUrl(LongUrl.AsMemory());
            _output.WriteLine($"Shortened URL: {shortUrlMemory}");

            shortUrlMemory.IsEmpty.ShouldBeFalse();
            shortUrlMemory.IsWhiteSpace().ShouldBeFalse();
            shortUrlMemory.Length.ShouldBeLessThan(8);

            var originalUrlMemory = urlShortener.RetrieveOriginalUrl(shortUrlMemory.ToArray());
            _output.WriteLine($"Retrieved URL: {originalUrlMemory}");

            originalUrlMemory.ShouldNotBeNullOrEmpty();
            originalUrlMemory.ShouldBe(LongUrl);

            //originalUrlMemory.IsEmpty.ShouldBeFalse();
            //originalUrlMemory.IsWhiteSpace().ShouldBeFalse();
            //originalUrlMemory.ToArray().ShouldBe(LongUrl.ToCharArray());
        }
        else
        {
            Skip.If(true);
        }
    }

    private static IUrlShortener TypeFactory(Type repositoryType, Type shortenerType)
    {
        var repository = Activator.CreateInstance(repositoryType) as IUrlRepository;
        repository.ShouldNotBeNull();
        var shortener = Activator.CreateInstance(shortenerType, new object[]{repository}) as IUrlShortener;
        shortener.ShouldNotBeNull();

        return shortener;
    }
}