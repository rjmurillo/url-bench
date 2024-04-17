#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Text;
using Contracts;
using IdGen;
using MurmurHash;

namespace Logic.Shorteners;

public class Murmur3HashUrlShortener(uint seed)
    : IUrlShortener
{
    private readonly IdGenerator _generator = new(0, new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));

    public Murmur3HashUrlShortener()
        : this(0U)
    {
    }

    public string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        Span<byte> inputBuffer = stackalloc byte[Encoding.Unicode.GetMaxByteCount(originalUrl.Length + 20)];
        Span<char> charBuffer = MemoryMarshal.Cast<byte, char>(inputBuffer); // Reinterpret the byte buffer as char buffer

        // Directly use Snowflake ID as a part of the URL for hashing
        var id = _generator.CreateId();
        id.TryFormat(charBuffer, out var charsWrittenId);
        originalUrl.AsSpan().CopyTo(charBuffer[charsWrittenId..]); // Append original URL after ID

        // Encode the char buffer containing the ID and the URL into the input buffer
        var bytesUsed = Encoding.Unicode.GetBytes(charBuffer[..(charsWrittenId + originalUrl.Length)], inputBuffer);
        ReadOnlySpan<byte> inputSpan = inputBuffer[..bytesUsed];

        // Calculate hash and convert to Base62
        var value = MurmurHash3.Hash32(ref inputSpan, seed);

        Span<char> base62Buffer = stackalloc char[Constants.MaxLength];
        Convert.AsBase62String(value, ref base62Buffer);
        return base62Buffer.ToString();
    }
}

#endif