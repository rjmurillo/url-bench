using Akka.Actor;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

/// <summary>
/// IUrlManager implementation that delegates operations to Akka.NET actors.
/// </summary>
public sealed class AkkaUrlManager : IUrlManagerSpan
{
    private readonly IActorRef _aggregate;
    private readonly IUrlShortener? _shortener;
    private readonly TimeSpan _timeout;

    public AkkaUrlManager(IActorRef aggregate, IActorRef querySame, IUrlShortener? shortener = null, TimeSpan? timeout = null)
    {
        _aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
        _shortener = shortener;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public string ShortenAndSave(string longUrl)
    {
        // Use Ask to ensure replies are delivered and avoid dead letters
        var resp = _aggregate.Ask(new ShortenUrl(longUrl), _timeout).GetAwaiter().GetResult();
        return resp switch
        {
            ShortenUrlAccepted ok => ok.ShortCode,
            ShortenUrlRejected rej => throw new InvalidOperationException(rej.Reason),
            _ => throw new InvalidOperationException("Unexpected response from aggregate")
        };
    }

    public string GetOriginalUrl(string shortUrl)
    {
        var response = _aggregate.Ask(new GetUrlByCode(shortUrl), _timeout).GetAwaiter().GetResult();
        return response switch
        {
            UrlDetails details => details.OriginalUrl,
            UrlNotFound => string.Empty,
            _ => string.Empty
        };
    }

    public string GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl)
    {
        return GetOriginalUrl(shortUrl.ToString());
    }
}
