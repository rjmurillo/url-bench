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
    /// <remarks>
    /// This lets us be a little smarter about the allocation. We don't always need the full length as a buffer,
    /// and if we know the length we can build the buffer in the correct order and avoid the reversal at the end.
    /// Uses binary search pattern for optimal branch prediction and reduced cyclomatic complexity.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DetermineBufferLength(ulong value)
    {
        // Handle the most common case first - values requiring 1-4 digits
        if (value < Constants.Base62Power4)
        {
            if (value < Constants.Base62Power2)
            {
                return value < Constants.Base62Power1 ? 1 : 2;
            }

            return value < Constants.Base62Power3 ? 3 : 4;
        }

        // Handle larger values requiring 5-8 digits
        if (value < Constants.Base62Power8)
        {
            if (value < Constants.Base62Power6)
            {
                return value < Constants.Base62Power5 ? 5 : 6;
            }

            return value < Constants.Base62Power7 ? 7 : 8;
        }

        // Fallback for values larger than Base62Power8
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
