using Contracts;

namespace Logic.Shorteners;

public class IncrementingIntegerUrlShortener(ulong initialValue) : IUrlShortener
{
    public IncrementingIntegerUrlShortener()
        : this(0UL)
    {
    }

    public string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        // Safely increment the initialValue using thread-safe operation to ensure that
        // no two threads can get the same value at the same time.
        // This is crucial in a multithreaded environment to prevent duplicate values.
        // The 'Interlocked.Increment' method increments initialValue atomically and returns the new value.
        // We subtract 1 because 'Interlocked.Increment' returns the incremented value,
        // but we need the starting index value for this operation.
        var value = Interlocked.Increment(ref initialValue) - 1UL;

        // Convert the numeric value to a base62 string. Base62 encoding uses characters 0-9, A-Z, and a-z.
        // This encoding helps in generating a shorter URL identifier from large numeric values.
        // For example, the first short URL will be "A", the second will be "B", and so on.
        Span<char> base62Buffer = stackalloc char[Math.Min(Convert.DetermineBufferLength(value), Constants.MaxSlugLength)];
        Convert.AsBase62String(value, ref base62Buffer);

        return base62Buffer.ToString();
    }
}