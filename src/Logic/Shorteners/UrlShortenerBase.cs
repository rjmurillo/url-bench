using Contracts;

namespace Logic.Shorteners;

public abstract class UrlShortenerBase : IUrlShortener
{
    protected UrlShortenerBase(IUrlRepository urlRepository)
    {
        ArgumentNullException.ThrowIfNull(urlRepository);

        Repository = urlRepository;
    }

    protected IUrlRepository Repository { get; }

    public abstract string ShortenUrl(string originalUrl);
}
