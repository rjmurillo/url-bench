#if NET8_0_OR_GREATER

using System.Security.Cryptography;
using System.Text;
using Contracts;
using IdGen;

namespace Logic.Shorteners;



public class Sha256HashUrlShortener()
    : IUrlShortener, IDisposable
{
    private SHA256 _sha256 = SHA256.Create();
    private readonly IdGenerator _generator = new(0, new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));
    private bool _disposed;

    public string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        // Generating a unique ID for each URL to ensure uniqueness
        var uniqueId = _generator.CreateId();

        // Encode the unique ID and URL together to ensure each input to SHA256 is unique
        Span<byte> urlBuffer = stackalloc byte[Encoding.Unicode.GetMaxByteCount(originalUrl.Length + 32)];
        var bytesWritten = Encoding.Unicode.GetBytes(uniqueId + originalUrl, urlBuffer);

        Span<byte> hashBuffer = stackalloc byte[32];  // SHA256 outputs 32 bytes, 256 bits
        _sha256.TryComputeHash(urlBuffer[..bytesWritten], hashBuffer, out _);

        // Base62 encode the SHA256 hash
        Span<char> base62Buffer = stackalloc char[Constants.MaxLength];  // Buffer for Base62 result, ensure it's appropriately sized
        Convert.AsBase62String(hashBuffer, ref base62Buffer);

        return base62Buffer.ToString();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sha256.Dispose();
                _sha256 = null!;
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

#endif