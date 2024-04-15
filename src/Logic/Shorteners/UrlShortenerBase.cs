using Contracts;
using Logic.Repositories;

namespace Logic.Shorteners;

public abstract class UrlShortenerBase : IUrlShortener
{
    protected UrlShortenerBase(IUrlRepository urlRepository)
    {
        ArgumentNullException.ThrowIfNull(urlRepository);

        Repository = urlRepository;
    }

    protected IUrlRepository Repository { get; }

    public virtual string RetrieveOriginalUrl(string shortUrl) => Repository.GetOriginalUrl(shortUrl);

    public abstract string ShortenUrl(string originalUrl);
}

public abstract class ReadOnlyMemoryUrlShortenerBase(ReadOnlyMemoryUrlRepositoryBase urlRepository)
    : UrlShortenerBase(urlRepository)
{
    // Allow access to optimized methods
    protected new ReadOnlyMemoryUrlRepositoryBase Repository { get; } = urlRepository;

    public override string RetrieveOriginalUrl(string shortUrl) => RetrieveOriginalUrl(shortUrl.AsMemory());

    public virtual string RetrieveOriginalUrl(ReadOnlyMemory<char> shortUrl) => Repository.GetOriginalUrl(shortUrl);

    public override string ShortenUrl(string originalUrl) => ShortenUrl(originalUrl.AsMemory()).ToString();
    public abstract ReadOnlySpan<char> ShortenUrl(ReadOnlyMemory<char> originalUrl);
}