namespace AkkaHost.Messages;

// Commands
public record ShortenUrl(string OriginalUrl);

// Events (Persisted)
public record UrlShortened(string OriginalUrl, string ShortCode, DateTimeOffset CreatedAt);

// Queries
public record GetUrlByCode(string ShortCode);

// Responses
public sealed record ShortenUrlAccepted(string ShortCode);
public sealed record ShortenUrlRejected(string Reason);

public record UrlDetails(string ShortCode, string OriginalUrl);
public record UrlNotFound(string ShortCode);
