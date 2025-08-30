using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Logic;

/// <summary>
/// Provides utility methods for converting values to Base62 string representations.
/// </summary>
public static class Base62Encoder
{
    private static readonly string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly ulong CharLength = 62UL;

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
    /// Converts a byte span to its Base62 string representation and writes it to the provided buffer.
    /// </summary>
    /// <param name="hash">The byte span to convert.</param>
    /// <param name="buffer">The buffer to write the result to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Encode(ReadOnlySpan<byte> hash, ref Span<char> buffer)
    {
        if (hash.Length < sizeof(ulong))
        {
            throw new ArgumentException("Hash must be at least 8 bytes", nameof(hash));
        }

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
        // Fast path for common small values to avoid division loop
        if (value < 62UL)
        {
            return 1;
        }

        if (value < 3844UL)
        {
            return 2;  // 62^2
        }

        if (value < 238328UL)
        {
            return 3; // 62^3
        }

        // For larger values, use the division loop
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
