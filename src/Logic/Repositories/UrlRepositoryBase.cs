using Contracts;

namespace Logic.Repositories;

public abstract class UrlRepositoryBase : IUrlRepository
{
    public abstract string GetOriginalUrl(string shortUrl);

    public abstract bool SaveUrlMapping(string shortUrl, string originalUrl);
}

public abstract class ReadOnlyMemoryUrlRepositoryBase : UrlRepositoryBase
{
    public override string GetOriginalUrl(string shortUrl) => GetOriginalUrl(shortUrl.AsMemory());

    public abstract string GetOriginalUrl(ReadOnlyMemory<char> shortUrl);

    public override bool SaveUrlMapping(string shortUrl, string originalUrl) => SaveUrlMapping(shortUrl.AsMemory(), originalUrl);
    public abstract bool SaveUrlMapping(ReadOnlyMemory<char> shortUrl, string originalUrl);
}