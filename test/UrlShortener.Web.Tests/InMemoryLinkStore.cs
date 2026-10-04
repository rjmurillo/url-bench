using System.Collections.Concurrent;
using UrlShortener.Web;

namespace UrlShortener.Web.Tests;

internal sealed class InMemoryLinkStore : ILinkStore
{
    private readonly ConcurrentDictionary<string, string> _links = new(StringComparer.Ordinal);

    public int Reads { get; private set; }

    public Exception? Failure { get; set; }

    public void Seed(string slug, string url) => _links[slug] = url;

    public string? Peek(string slug) => _links.TryGetValue(slug, out string? url) ? url : null;

    public Task<string?> GetAsync(string slug, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<string?>(Failure);
        }

        Reads++;
        return Task.FromResult(_links.TryGetValue(slug, out string? url) ? url : null);
    }

    public Task<string?> TryAddAsync(string slug, string url, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<string?>(Failure);
        }

        string stored = _links.GetOrAdd(slug, url);
        return Task.FromResult(ReferenceEquals(stored, url) ? null : stored);
    }
}
