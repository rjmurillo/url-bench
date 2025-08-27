using Akka.Actor;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

/// <summary>
/// Command-side actor responsible for generating and persisting URL mappings via an IUrlRepository.
/// </summary>
public sealed class UrlWriteActor : ReceiveActor
{
    private readonly IUrlShortener _shortener;
    private readonly IUrlRepository _repository;

    public UrlWriteActor(IUrlShortener shortener, IUrlRepository repository)
    {
        _shortener = shortener ?? throw new ArgumentNullException(nameof(shortener));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Receive<ShortenUrl>(HandleShortenUrl);
        Receive<SaveShortened>(HandleSaveShortened);
    }

    private void HandleShortenUrl(ShortenUrl cmd)
    {
        var replyTo = Sender;
        var shortCode = _shortener.ShortenUrl(cmd.OriginalUrl);

        if (shortCode.Length == 0)
        {
            replyTo.Tell(new ShortenUrlAccepted(string.Empty));
            return;
        }

        if (TryPersist(shortCode, cmd.OriginalUrl))
        {
            replyTo.Tell(new ShortenUrlAccepted(shortCode));
        }
        else
        {
            replyTo.Tell(new ShortenUrlRejected($"Collision detected for short code '{shortCode}'"));
        }
    }

    private void HandleSaveShortened(SaveShortened cmd)
    {
        var replyTo = Sender;
        if (cmd.ShortCode.Length == 0)
        {
            replyTo.Tell(new ShortenUrlAccepted(string.Empty));
            return;
        }

        if (TryPersist(cmd.ShortCode, cmd.OriginalUrl))
        {
            replyTo.Tell(new ShortenUrlAccepted(cmd.ShortCode));
        }
        else
        {
            replyTo.Tell(new ShortenUrlRejected($"Collision detected for short code '{cmd.ShortCode}'"));
        }
    }

    private bool TryPersist(string shortCode, string originalUrl)
    {
        // Optimistic insert: expect the key to not exist
        var ok = _repository.TrySaveUrlMapping(shortCode, originalUrl, expectedValue: null, out var currentValue);
        if (ok)
            return true;

        // Idempotent accept if the existing value matches the request
        return string.Equals(currentValue, originalUrl, StringComparison.Ordinal);
    }
}
