using Contracts;
using IdGen;

namespace Logic.Shorteners;

public class SnowflakeUrlShortener(int generatorId) : IUrlShortener
{
    private readonly IdGenerator _generator = new(generatorId, new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));

    public SnowflakeUrlShortener()
        :this(0)
    {
    }

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
        Span<char> base62Buffer = stackalloc char[Constants.MaxLength];
        Convert.AsBase62String((ulong)id, ref base62Buffer);

        return base62Buffer.ToString();
    }
}