using System.Buffers;
using System.Runtime.CompilerServices;

namespace Logic;

public static class Convert
{
    private const string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const ulong CharLength = 62UL;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsBase62String(ulong value)
    {
        var bufferLength = DetermineBufferLength(value);

        var bufferIndex = bufferLength - 1;
        var arrayToReturnToPool = ArrayPool<char>.Shared.Rent(bufferLength);
        Span<char> buffer = arrayToReturnToPool;
        try
        {
            do
            {
                buffer[bufferIndex--] = Base62Chars[(int)(value % CharLength)];
                value /= CharLength;
            } while (value > 0UL);

            return buffer[..bufferLength];
        }
        finally
        {
            ArrayPool<char>.Shared.Return(arrayToReturnToPool);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int DetermineBufferLength(ulong value)
    {
        // Let's be a little smarter about the allocation
        //
        // We don't always need the full Length as a buffer
        // and if we know the length we can build the buffer
        // in the correct order and avoid the reversal at the end
        var size = value;
        var bufferLength = 0;
        do
        {
            bufferLength++;
            size /= CharLength;
        } while (size > 0UL);

        return bufferLength;
    }

    public static void AsBase62String(ulong value, ref Span<char> buffer)
    {
        var bufferIndex = buffer.Length - 1;
        do
        {
            buffer[bufferIndex--] = Base62Chars[(int)(value % CharLength)];
            value /= CharLength;
        } while (value > 0UL && bufferIndex >= 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsBase62String(Span<byte> hash)
    {
        var index = BitConverter.ToUInt64(hash) % Constants.Base62Power8;
        return AsBase62String(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AsBase62String(Span<byte> hash, ref Span<char> buffer)
    {
        var index = BitConverter.ToUInt64(hash) % Constants.Base62Power8;
        AsBase62String(index, ref buffer);
    }
}