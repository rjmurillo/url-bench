namespace AkkaHost.Messages;

// Commands
public record struct ShortenUrl(string OriginalUrl);

// Events (Persisted)
public record struct UrlShortened(string OriginalUrl, string ShortCode, DateTimeOffset CreatedAt);

// Queries
public record struct GetUrlByCode(string ShortCode);

// Responses
public sealed record ShortenUrlAccepted(string ShortCode);
public sealed record ShortenUrlRejected(string Reason);

public record struct UrlDetails(string ShortCode, string OriginalUrl);
public record struct UrlNotFound(string ShortCode);
