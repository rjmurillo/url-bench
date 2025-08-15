using Contracts;

namespace Logic.Repositories;

public abstract class UrlRepositoryBase : IUrlRepository
{
    public abstract string GetOriginalUrl(string shortUrl);

    public abstract bool SaveUrlMapping(string shortUrl, string originalUrl);
}