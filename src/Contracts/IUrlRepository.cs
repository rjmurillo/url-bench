namespace Contracts
{
    public interface IUrlRepository
    {
        string GetOriginalUrl(string shortUrl);
        bool SaveUrlMapping(string shortUrl, string originalUrl);
    }
}