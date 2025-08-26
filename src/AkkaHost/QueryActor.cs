using Akka.Actor;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

public sealed class QueryActor : ReceiveActor
{
    private readonly IUrlRepository _repository;

    public QueryActor(IUrlRepository repository)
    {
        _repository = repository;
        Receive<GetUrlByCode>(HandleGetByShortCode);
    }

    private void HandleGetByShortCode(GetUrlByCode query)
    {
        var originalUrl = _repository.GetOriginalUrl(query.ShortCode);
        if (originalUrl.Length == 0)
        {
            Sender.Tell(new UrlNotFound(query.ShortCode));
        }
        else
        {
            Sender.Tell(new UrlDetails(query.ShortCode, originalUrl));
        }
    }
}