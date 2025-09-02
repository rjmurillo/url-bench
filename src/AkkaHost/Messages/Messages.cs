namespace AkkaHost.Messages;

// Commands
public sealed record ShortenUrl(string OriginalUrl);

public sealed record SaveShortened(string OriginalUrl, string ShortCode);

// Queries
public record GetUrlByCode(string ShortCode);

// Responses
public sealed record ShortenUrlAccepted(string ShortCode);

public sealed record ShortenUrlRejected(string Reason);

public sealed record UrlDetails(string ShortCode, string OriginalUrl);

public sealed record UrlNotFound(string ShortCode);
