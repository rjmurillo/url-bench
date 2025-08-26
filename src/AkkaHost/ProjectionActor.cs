using Akka.Actor;
using Akka.Event;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

public sealed class ProjectionActor : ReceiveActor
{
    private readonly IUrlRepository _repository;
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    public ProjectionActor(IUrlRepository repository)
    {
        _repository = repository;

        Receive<UrlShortened>(HandleUrlShortened);
    }

    private void HandleUrlShortened(UrlShortened @event)
    {
        _repository.SaveUrlMapping(@event.ShortCode, @event.OriginalUrl);

        _logger.Info("Persisted {0} as {1}", @event.OriginalUrl, @event.ShortCode);
    }
}