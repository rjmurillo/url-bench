using Akka.Actor;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

/// <summary>
/// Query-side actor responsible for serving read requests against an IUrlRepository.
/// </summary>
public sealed class UrlReadActor : ReceiveActor
{
    private readonly IUrlRepository _repository;

    public UrlReadActor(IUrlRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Receive<GetUrlByCode>(HandleGet);
    }

    private void HandleGet(GetUrlByCode q)
    {
        var url = _repository.GetOriginalUrl(q.ShortCode);
        if (!string.IsNullOrEmpty(url))
        {
            Sender.Tell(new UrlDetails(q.ShortCode, url));
        }
        else
        {
            Sender.Tell(new UrlNotFound(q.ShortCode));
        }
    }
}
