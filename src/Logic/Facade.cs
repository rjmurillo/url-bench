using Contracts;

namespace Logic;

public class Facade(IUrlShortener? shortener, IUrlRepository? repository)
{
    private readonly IUrlShortener _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
    private readonly IUrlRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public string ShortenAndSave(string longUrl)
    {
        var shortUrl = _shortener.ShortenUrl(longUrl);
        if (_repository.SaveUrlMapping(shortUrl, longUrl))
        {
            return shortUrl;
        }

        throw new InvalidOperationException("Failed to save URL mapping.");
    }

    public string GetOriginalUrl(string shortUrl)
    {
        return _repository.GetOriginalUrl(shortUrl) ?? string.Empty;
    }
}