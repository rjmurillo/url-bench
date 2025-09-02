using Akka.Actor;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

/// <summary>
/// IUrlManager implementation that delegates operations to Akka.NET actors.
/// </summary>
public sealed class AkkaUrlManager : IUrlManagerSpan
{
    private readonly IActorRef _writer;
    private readonly IActorRef _reader;
    private readonly TimeSpan _timeout;

    public AkkaUrlManager(IActorRef writer, IActorRef reader, TimeSpan? timeout = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

        if (timeout is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout.Value, TimeSpan.Zero);
        }

        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public string ShortenAndSave(string longUrl)
    {
        // Use Ask to ensure replies are delivered and avoid dead letters
        var resp = _writer.Ask(new ShortenUrl(longUrl), _timeout).GetAwaiter().GetResult();
        return resp switch
        {
            ShortenUrlAccepted ok => ok.ShortCode,
            ShortenUrlRejected rej => throw new InvalidOperationException(rej.Reason),
            _ => throw new InvalidOperationException("Unexpected response from writer"),
        };
    }

    public string GetOriginalUrl(string shortUrl)
    {
        var response = _reader.Ask(new GetUrlByCode(shortUrl), _timeout).GetAwaiter().GetResult();
        return response switch
        {
            UrlDetails details => details.OriginalUrl,
            UrlNotFound => string.Empty,
            _ => string.Empty,
        };
    }

    public string GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl)
    {
        return GetOriginalUrl(shortUrl.ToString());
    }
}
