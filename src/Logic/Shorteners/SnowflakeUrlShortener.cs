using Contracts;
using IdGen;

namespace Logic.Shorteners;

/// <summary>
/// A URL shortener that generates short URLs using the Snowflake ID algorithm.
/// </summary>
/// <param name="generatorId">The generator ID to use for the Snowflake algorithm.</param>
public class SnowflakeUrlShortener(int generatorId) : IUrlShortener
{
    private readonly IdGenerator _generator = new(generatorId, new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));

    /// <summary>
    /// Initializes a new instance of the SnowflakeUrlShortener class with a generator ID of 0.
    /// </summary>
    public SnowflakeUrlShortener()
        : this(0)
    {
    }

    /// <summary>
    /// Shortens the specified original URL by generating a unique short URL using the Snowflake algorithm.
    /// </summary>
    /// <param name="originalUrl">The original (long) URL to shorten. May be any valid URL string.</param>
    /// <returns>
    /// A case-sensitive slug consisting exclusively of characters.
    /// The value contains no scheme, host, or path prefix and has no leading or trailing slashes.
    /// The slug is not URL-encoded and is intended to be used as a single path segment
    /// (e.g., compose the full URL externally via $"https://sho.rt/{slug}").
    /// Returns an empty string if <paramref name="originalUrl"/> is null, empty, or whitespace.
    /// </returns>
    public string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        // Generate a unique ID using the Snowflake algorithm.
        // The Snowflake algorithm generates a unique 64-bit ID based on the current timestamp,
        // a unique worker ID, and a sequence number.
        var id = _generator.CreateId();

        // Convert the unique ID to a base62 string.
        Span<char> base62Buffer = stackalloc char[Constants.MaxSlugLength];
        Convert.AsBase62String((ulong)id, ref base62Buffer);

        return base62Buffer.ToString();
    }
}