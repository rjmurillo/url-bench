using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        // Decide between stackalloc and ArrayPool
        if (byteCount <= Constants.MaxStackSize)
        {
            Span<byte> inputBuffer = stackalloc byte[byteCount];
            return ProcessUrl(originalUrl, inputBuffer);
        }

        var arrayFromPool = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var poolBuffer = arrayFromPool.AsSpan(0, byteCount);
            return ProcessUrl(originalUrl, poolBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    // This is unlikely to inline (too many IL bytes)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ProcessUrl(string originalUrl, Span<byte> inputBuffer)
    {
        // Copy the original URL into the buffer
        var bytesUsed = Encoding.Unicode.GetBytes(originalUrl, inputBuffer);

        // SHA256 outputs 32 bytes, 256 bits
        Span<byte> hashBuffer = stackalloc byte[32];
        var success = _sha256.TryComputeHash(inputBuffer[..bytesUsed], hashBuffer, out _);

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