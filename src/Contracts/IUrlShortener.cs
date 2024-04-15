namespace Contracts
{
    public interface IUrlShortener
    {
        string ShortenUrl(string originalUrl);
        string RetrieveOriginalUrl(string shortUrl);
    }
}