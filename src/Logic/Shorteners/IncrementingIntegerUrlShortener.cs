using Contracts;

namespace Logic.Shorteners;

/// <summary>
/// A URL shortener that generates short URLs using an incrementing integer value.
/// </summary>
/// <param name="initialValue">The initial value to start incrementing from.</param>
public class IncrementingIntegerUrlShortener(ulong initialValue) : IUrlShortener
{
    /// <summary>
    /// The current value for incrementing. This field is thread-safe and gets atomically incremented.
    /// </summary>
    private ulong _currentValue = initialValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementingIntegerUrlShortener"/> class with an initial value of 0.
    /// </summary>
    public IncrementingIntegerUrlShortener()
        : this(0UL)
    {
    }

    /// <summary>
    /// Shortens the specified original URL by generating a unique short URL using an incrementing integer.
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

        // Safely increment the current value using thread-safe operation to ensure that
        // no two threads can get the same value at the same time.
        // This is crucial in a multithreaded environment to prevent duplicate values.
        // The 'Interlocked.Increment' method increments _currentValue atomically and returns the new value.
        // We subtract 1 because 'Interlocked.Increment' returns the incremented value,
        // but we need the starting index value for this operation.
        var value = Interlocked.Increment(ref _currentValue) - 1UL;

        // Convert the numeric value to a base62 string. Base62 encoding uses characters 0-9, A-Z, and a-z.
        // This encoding helps in generating a shorter URL identifier from large numeric values.
        // For example, the first short URL will be "A", the second will be "B", and so on.
        Span<char> base62Buffer = stackalloc char[Math.Min(Convert.DetermineBufferLength(value), Constants.MaxSlugLength)];
        Convert.AsBase62String(value, ref base62Buffer);

        return base62Buffer.ToString();
    }
}
