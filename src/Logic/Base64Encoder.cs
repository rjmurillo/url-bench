using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Logic;

/// <summary>
/// Provides utility methods for converting values to Base62 string representations.
/// </summary>
public static class Base64Encoder
{
    private static readonly string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly ulong CharLength = 62UL;

    /// <summary>
    /// Converts a 64-bit unsigned integer to its Base62 string representation.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The Base62 string representation of the value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> Encode(ulong value)
    {
        int bufferLength = DetermineBufferLength(value);

        int bufferIndex = bufferLength - 1;
        var arrayToReturnToPool = ArrayPool<char>.Shared.Rent(bufferLength);
#pragma warning disable ECS0900 // false positive
        Span<char> buffer = arrayToReturnToPool;
#pragma warning restore ECS0900
        try
        {
            do
            {
                buffer[bufferIndex--] = Base62Chars[(int)(value % CharLength)];
                value /= CharLength;
            }
            while (value > 0UL);

            return buffer[..bufferLength];
        }
        finally
        {
            ArrayPool<char>.Shared.Return(arrayToReturnToPool);
        }
    }

    /// <summary>
    /// Converts a 64-bit unsigned integer to its Base62 string representation and writes it to the provided buffer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="buffer">The buffer to write the result to.</param>
    public static void Encode(ulong value, ref Span<char> buffer)
    {
        int bufferIndex = buffer.Length - 1;
        do
        {
            buffer[bufferIndex--] = Base62Chars[(int)(value % CharLength)];
            value /= CharLength;
        }
        while (value > 0UL && bufferIndex >= 0);
    }

    /// <summary>
    /// Converts a byte span to its Base62 string representation.
    /// </summary>
    /// <param name="hash">The byte span to convert.</param>
    /// <returns>The Base62 string representation of the hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> Encode(Span<byte> hash)
    {
        ulong index = BinaryPrimitives.ReadUInt64LittleEndian(hash) % Constants.Base62Power8;
        return Encode(index);
    }

    /// <summary>
    /// Converts a byte span to its Base62 string representation and writes it to the provided buffer.
    /// </summary>
    /// <param name="hash">The byte span to convert.</param>
    /// <param name="buffer">The buffer to write the result to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Encode(Span<byte> hash, ref Span<char> buffer)
    {
        ulong index = BinaryPrimitives.ReadUInt64LittleEndian(hash) % Constants.Base62Power8;
        Encode(index, ref buffer);
    }

    /// <summary>
    /// Determines the required buffer length for converting a value to Base62 string representation.
    /// </summary>
    /// <param name="value">The value to determine buffer length for.</param>
    /// <returns>The required buffer length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DetermineBufferLength(ulong value)
    {
        // Let's be a little smarter about the allocation
        //
        // We don't always need the full Length as a buffer
        // and if we know the length we can build the buffer
        // in the correct order and avoid the reversal at the end
        ulong size = value;
        int bufferLength = 0;
        do
        {
            bufferLength++;
            size /= CharLength;
        }
        while (size > 0UL);

        return bufferLength;
    }
}
