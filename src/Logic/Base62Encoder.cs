using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Logic;

/// <summary>
/// Provides utility methods for converting values to Base62 string representations.
/// </summary>
public static class Base62Encoder
{
#pragma warning disable ECS0200 // Consider using readonly instead of const for better flexibility
    private const string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const ulong CharLength = 62UL;
#pragma warning restore ECS0200 // Consider using readonly instead of const for better flexibility

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
            // Compute quotient = value / 62 using multiplication by reciprocal (magic multiplier)
            ulong q = MultiplyHigh(value, (ulong.MaxValue / CharLength) + 1UL);

            // t = q * 62 (will not overflow for valid q)
            ulong t = q * CharLength;

            // If t > value then q is too large; decrement until t <= value (usually at most once)
            if (t > value)
            {
                do
                {
                    q--;
                    t -= CharLength;
                }
                while (t > value);
            }

            // remainder now in [0,62)
            ulong rem = value - t;

            buffer[bufferIndex--] = Base62Chars[(int)rem];
            value = q;
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
    /// Converts a 64-bit unsigned integer to its Base62 string representation efficiently.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The Base62 string representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EncodeToString(ulong value)
    {
        int length = DetermineBufferLength(value);
        return string.Create(length, value, static (span, val) =>
        {
            Encode(val, ref span);
        });
    }

    /// <summary>
    /// Converts a 64-bit unsigned integer to its Base62 string representation efficiently,
    /// with maximum length constraint.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="maxLength">The maximum length of the result string.</param>
    /// <returns>The Base62 string representation, truncated to maxLength if necessary.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EncodeToString(ulong value, int maxLength)
    {
        int length = Math.Min(DetermineBufferLength(value), maxLength);
        return string.Create(length, value, static (span, val) =>
        {
            Encode(val, ref span);
        });
    }

    /// <summary>
    /// Converts a byte array to its Base62 string representation efficiently,
    /// with maximum length constraint.
    /// </summary>
    /// <param name="bytes">The byte array to convert.</param>
    /// <param name="maxLength">The maximum length of the result string.</param>
    /// <returns>The Base62 string representation, truncated to maxLength if necessary.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EncodeToString(ReadOnlySpan<byte> bytes, int maxLength)
    {
        if (bytes.Length < sizeof(ulong))
        {
            throw new ArgumentException("Hash must be at least 8 bytes", nameof(bytes));
        }

        ulong index = BinaryPrimitives.ReadUInt64LittleEndian(bytes) % Constants.Base62Power8;
        int bufferLength = DetermineBufferLength(index);
        int length = Math.Min(bufferLength, maxLength);

        return string.Create(length, index, static (span, value) =>
        {
            Encode(value, ref span);
        });
    }

    /// <summary>
    /// Converts a byte span to its Base62 string representation efficiently.
    /// </summary>
    /// <param name="hash">The byte span to convert.</param>
    /// <returns>The Base62 string representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EncodeToString(ReadOnlySpan<byte> hash)
    {
        if (hash.Length < sizeof(ulong))
        {
            throw new ArgumentException("Hash must be at least 8 bytes", nameof(hash));
        }

        ulong index = BinaryPrimitives.ReadUInt64LittleEndian(hash) % Constants.Base62Power8;
        return EncodeToString(index);
    }

    /// <summary>
    /// Determines the required buffer length for converting a value to Base62 string representation.
    /// </summary>
    /// <param name="value">The value to determine buffer length for.</param>
    /// <returns>The required buffer length.</returns>
    /// <remarks>
    /// This lets us be a little smarter about the allocation. We don't always need the full length as a buffer,
    /// and if we know the length we can build the buffer in the correct order and avoid the reversal at the end.
    /// Uses a threshold-based early-return allocation strategy (two-tier range checks) that performs at most
    /// three comparisons for the common cases to determine length and avoid an extra buffer reversal.
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

    // Private helpers (kept after public API to satisfy style rules)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MultiplyHigh(ulong x, ulong y)
    {
        const ulong mask32 = 0xFFFFFFFFUL;
        ulong x0 = x & mask32;
        ulong x1 = x >> 32;
        ulong y0 = y & mask32;
        ulong y1 = y >> 32;

        ulong p0 = x0 * y0;
        ulong p1 = x0 * y1;
        ulong p2 = x1 * y0;
        ulong p3 = x1 * y1;

        // Combine partial products to form the high 64 bits of the 128-bit product
        ulong t = (p0 >> 32) + (p1 & mask32) + (p2 & mask32);
        ulong high = p3 + (p1 >> 32) + (p2 >> 32) + (t >> 32);
        return high;
    }
}
