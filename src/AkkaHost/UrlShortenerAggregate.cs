using Akka.Actor;
using Akka.Persistence;

using AkkaHost.Messages;

using Logic.Contracts;

namespace AkkaHost;

public sealed class UrlShortenerAggregate : ReceivePersistentActor
{
    private readonly IUrlShortener _shortener;

    // In-memory state (shortCode -> originalUrl). Rebuilt from journal on recovery.
    private readonly Dictionary<string, string> _state = new(StringComparer.Ordinal);

    public UrlShortenerAggregate(IUrlShortener shortener)
    {
        _shortener = shortener;

        Command<ShortenUrl>(HandleShortenUrl);
        Command<SaveShortened>(HandleSaveShortened);
        Command<GetUrlByCode>(HandleGetUrlByCode);

        Recover<UrlShortened>(Apply);
    }

    public override string PersistenceId => "url-shortener-aggregate";

    private void Apply(UrlShortened evt)
    {
        _state[evt.ShortCode] = evt.OriginalUrl;
    }

    private static bool IsIdempotent(string existing, string candidate)
        => string.Equals(existing, candidate, StringComparison.Ordinal);

    private bool CanAccept(string shortCode, string originalUrl)
    {
        if (!_state.TryGetValue(shortCode, out var existing))
            return true;

        return IsIdempotent(existing, originalUrl);
    }

    private static bool HasRealSender(IActorRef sender)
        => !ReferenceEquals(sender, ActorRefs.NoSender) && !ReferenceEquals(sender, ActorRefs.Nobody);

    private void HandleGetUrlByCode(GetUrlByCode q)
    {
        if (_state.TryGetValue(q.ShortCode, out var url) && url.Length != 0)
        {
            Sender.Tell(new UrlDetails(q.ShortCode, url));
        }
        else
        {
            Sender.Tell(new UrlNotFound(q.ShortCode));
        }
    }

    private void HandleSaveShortened(SaveShortened arg)
    {
        var replyTo = Sender;
        var e = new UrlShortened(arg.OriginalUrl, arg.ShortCode, DateTimeOffset.UtcNow);

        if (CanAccept(e.ShortCode, e.OriginalUrl))
        {
            // Update state first for max throughput; persist async
            Apply(e);
            if (HasRealSender(replyTo))
            {
                replyTo.Tell(new ShortenUrlAccepted(e.ShortCode));
            }
            Persist(e, _ => { /* no-op */ });
        }
        else
        {
            if (HasRealSender(replyTo))
            {
                replyTo.Tell(new ShortenUrlRejected($"Collision detected for short code '{e.ShortCode}'"));
            }
        }
    }

    private void HandleShortenUrl(ShortenUrl arg)
    {
        var replyTo = Sender;

        var shortCode = _shortener.ShortenUrl(arg.OriginalUrl);
        if (shortCode.Length == 0)
        {
            if (HasRealSender(replyTo))
            {
                replyTo.Tell(new ShortenUrlAccepted(string.Empty));
            }

            return;
        }

        if (CanAccept(shortCode, arg.OriginalUrl))
        {
            var e = new UrlShortened(arg.OriginalUrl, shortCode, DateTimeOffset.UtcNow);
            Apply(e);
            if (HasRealSender(replyTo)) replyTo.Tell(new ShortenUrlAccepted(e.ShortCode));
            Persist(e, _ => { /* no-op */ });
        }
        else
        {
            if (HasRealSender(replyTo))
            {
                replyTo.Tell(new ShortenUrlRejected($"Collision detected for short code '{shortCode}'"));
            }
        }
    }
}