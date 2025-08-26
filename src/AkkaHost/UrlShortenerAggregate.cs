using Akka.Actor;
using Akka.Persistence;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

public sealed class UrlShortenerAggregate : ReceivePersistentActor
{
    private readonly IActorRef _projection;
    private readonly IUrlShortener _shortener;
    private readonly IUrlRepository _repository;

    public UrlShortenerAggregate(IActorRef projection, IUrlShortener shortener, IUrlRepository repository)
    {
        _projection = projection;
        _shortener = shortener;
        _repository = repository;

        Command<ShortenUrl>(HandleShortenUrl);
        Recover<UrlShortened>(Apply);
    }

    private bool Apply(UrlShortened obj)
    {
        // Try optimistic save (expecting no existing mapping)
        var success = _repository.TrySaveUrlMapping(obj.ShortCode, obj.OriginalUrl, expectedValue: null, out var currentValue);

        if (success)
        {
            // Successfully saved new mapping
            return true;
        }

        // Check if the collision is with the same URL (idempotency)
        if (currentValue is not null && string.Equals(currentValue, obj.OriginalUrl, StringComparison.Ordinal))
        {
            // Idempotent case: same input returns the same slug
            return true;
        }

        return false;
    }

    public override string PersistenceId => "url-shortener-aggregate";

    private void HandleShortenUrl(ShortenUrl arg)
    {
        var shortUrl = _shortener.ShortenUrl(arg.OriginalUrl);
        var e = new UrlShortened(arg.OriginalUrl, shortUrl, DateTimeOffset.UtcNow);

        Persist(e, HandlePersist);
    }

    private void HandlePersist(UrlShortened obj)
    {
        Apply(obj);
    }
}