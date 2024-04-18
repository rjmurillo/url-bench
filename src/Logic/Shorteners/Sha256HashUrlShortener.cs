#if NET8_0_OR_GREATER

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Contracts;

namespace Logic.Shorteners;

public class Sha256HashUrlShortener()
    : IUrlShortener, IDisposable
{
    private SHA256 _sha256 = SHA256.Create();
    private bool _disposed;

    public string ShortenUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return string.Empty;
        }

        var byteCount = Encoding.Unicode.GetMaxByteCount(originalUrl.Length);

        Span<byte> inputBuffer = byteCount <= Constants.MaxStackSize
                                ? stackalloc byte[byteCount]
                                : new byte[byteCount];
        Span<char> charBuffer = MemoryMarshal.Cast<byte, char>(inputBuffer); // Reinterpret the byte buffer as char buffer

        originalUrl.AsSpan().CopyTo(charBuffer); // Copy the original URL into the char buffer
        var bytesUsed = Encoding.Unicode.GetBytes(charBuffer, inputBuffer);

        Span<byte> hashBuffer = stackalloc byte[32];  // SHA256 outputs 32 bytes, 256 bits
        var success = _sha256.TryComputeHash(inputBuffer[..bytesUsed], hashBuffer, out var value);

        Debug.Assert(success, "Could not create the hash from input.");

        // Base62 encode the SHA256 hash
        Span<char> base62Buffer = stackalloc char[Constants.MaxSlugLength];
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