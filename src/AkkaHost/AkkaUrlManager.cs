using Akka.Actor;
using AkkaHost.Messages;
using Logic.Contracts;

namespace AkkaHost;

/// <summary>
/// IUrlManager implementation that delegates operations to Akka.NET actors.
/// Provides both synchronous and asynchronous APIs. Prefer the async members for
/// non-blocking behavior and deterministic timeout/cancellation.
/// </summary>
public sealed class AkkaUrlManager : IUrlManagerSpan
{
    private readonly IActorRef _writer;
    private readonly IActorRef _reader;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Creates a new instance of <see cref="AkkaUrlManager"/>.
    /// </summary>
    /// <param name="writer">Actor reference that handles write operations.</param>
    /// <param name="reader">Actor reference that handles read operations.</param>
    /// <param name="timeout">Optional default timeout applied to operations; defaults to 30 seconds and must be greater than zero.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> or <paramref name="reader"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
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

    /// <summary>
    /// Shortens the specified URL and persists it, returning the short code.
    /// This synchronous method is a compatibility shim over <see cref="ShortenAndSaveAsync"/>.
    /// </summary>
    public string ShortenAndSave(string longUrl)
    {
        return ShortenAndSaveAsync(longUrl).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the original URL associated with the specified short code.
    /// This synchronous method is a compatibility shim over <see cref="GetOriginalUrlAsync"/>.
    /// </summary>
    public string GetOriginalUrl(string shortUrl)
    {
        return GetOriginalUrlAsync(shortUrl).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves the original URL given a short code as a span. This is a thin shim over the string-based API.
    /// </summary>
    /// <param name="shortUrl">The short code as a read-only character span.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public string GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl)
    {
        return GetOriginalUrl(shortUrl.ToString());
    }

    /// <summary>
    /// Asynchronously shortens the specified URL and persists it, returning the short code.
    /// Respects the manager's default timeout unless overridden at construction.
    /// Throws <see cref="TimeoutException"/> on timeout and <see cref="OperationCanceledException"/> on cancellation.
    /// </summary>
    /// <param name="longUrl">The original URL to shorten.</param>
    /// <param name="cancellationToken">An optional token to observe while awaiting the operation.</param>
    /// <returns>The generated short code.</returns>
    public async Task<string> ShortenAndSaveAsync(string longUrl, CancellationToken cancellationToken = default)
    {
        // Ask without internal timeout; enforce deterministically via WaitAsync
        var askTask = _writer.Ask(new ShortenUrl(longUrl), System.Threading.Timeout.InfiniteTimeSpan);
        var response = await askTask.WaitAsync(_timeout, cancellationToken).ConfigureAwait(false);
        return response switch
        {
            ShortenUrlAccepted ok => ok.ShortCode,
            ShortenUrlRejected rej => throw new InvalidOperationException(rej.Reason),
            _ => throw new InvalidOperationException("Unexpected response from writer"),
        };
    }

    /// <summary>
    /// Asynchronously retrieves the original URL associated with the specified short code.
    /// Respects the manager's default timeout unless overridden at construction.
    /// Throws <see cref="TimeoutException"/> on timeout and <see cref="OperationCanceledException"/> on cancellation.
    /// </summary>
    /// <param name="shortUrl">The short code.</param>
    /// <param name="cancellationToken">An optional token to observe while awaiting the operation.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    public async Task<string> GetOriginalUrlAsync(string shortUrl, CancellationToken cancellationToken = default)
    {
        var askTask = _reader.Ask(new GetUrlByCode(shortUrl), System.Threading.Timeout.InfiniteTimeSpan);
        var response = await askTask.WaitAsync(_timeout, cancellationToken).ConfigureAwait(false);
        return response switch
        {
            UrlDetails details => details.OriginalUrl,
            UrlNotFound => string.Empty,
            _ => string.Empty,
        };
    }
}
