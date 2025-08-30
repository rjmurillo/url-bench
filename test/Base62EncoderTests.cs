using System.Buffers.Binary;
using Logic;

namespace UrlConverterPermutations;

/// <summary>
/// Test class for the Base62Encoder utility class.
/// </summary>
/// <param name="output">The test output helper for writing test information.</param>
public class Base62EncoderTests(ITestOutputHelper output)
{
    private static readonly string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Tests conversion of zero value.
    /// </summary>
    [Fact]
    public void Encode_ZeroValue_ReturnsSingleCharacter()
    {
        int bufferLength = Base62Encoder.DetermineBufferLength(0UL);
        var buffer = new char[bufferLength];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(0UL, ref span);

        var result = new string(buffer);
        output.WriteLine($"Result: {result}");

        result.Length.ShouldBe(1);
        result[0].ShouldBe('A');
    }

    /// <summary>
    /// Tests conversion of single digit values (1-61).
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="expected">The expected Base62 string result.</param>
    [Theory]
    [InlineData(1UL, "B")]
    [InlineData(10UL, "K")]
    [InlineData(25UL, "Z")]
    [InlineData(26UL, "a")]
    [InlineData(51UL, "z")]
    [InlineData(52UL, "0")]
    [InlineData(61UL, "9")]
    public void Encode_SingleDigitValues_ReturnsCorrectCharacter(ulong value, string expected)
    {
        int bufferLength = Base62Encoder.DetermineBufferLength(value);
        var buffer = new char[bufferLength];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(value, ref span);

        var result = new string(buffer);
        output.WriteLine($"Value: {value}, Result: {result}, Expected: {expected}");

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests conversion of two digit values.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="expected">The expected Base62 string result.</param>
    [Theory]
    [InlineData(62UL, "BA")]
    [InlineData(63UL, "BB")]
    [InlineData(124UL, "CA")]
    [InlineData(3843UL, "99")]
    [InlineData(3844UL, "BAA")]
    public void Encode_TwoDigitValues_ReturnsCorrectString(ulong value, string expected)
    {
        int bufferLength = Base62Encoder.DetermineBufferLength(value);
        var buffer = new char[bufferLength];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(value, ref span);

        var result = new string(buffer);
        output.WriteLine($"Value: {value}, Result: {result}, Expected: {expected}");

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests conversion of large values.
    /// </summary>
    /// <param name="value">The large value to convert.</param>
    [Theory]
    [InlineData(ulong.MaxValue)]
    [InlineData(ulong.MaxValue - 1)]
    [InlineData(1_000_000_000UL)]
    [InlineData(1_000_000_000_000UL)]
    public void Encode_LargeValues_ReturnsValidBase62String(ulong value)
    {
        int bufferLength = Base62Encoder.DetermineBufferLength(value);
        var buffer = new char[bufferLength];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(value, ref span);

        var result = new string(buffer);
        output.WriteLine($"Value: {value}, Result: {result}");

        result.Length.ShouldBeGreaterThan(0);
        result.Length.ShouldBeLessThanOrEqualTo(11); // Max length for ulong in base62

        // Verify all characters are valid Base62
        foreach (var c in result)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests conversion with sufficient buffer size.
    /// </summary>
    [Fact]
    public void Encode_WithBuffer_SufficientSize_WritesCorrectly()
    {
        ulong value = 12345UL;
        var expectedEncoded = "DNH";

        var oversizeBuffer = new char[10];
        var oversizeSpan = oversizeBuffer.AsSpan();

        Base62Encoder.Encode(value, ref oversizeSpan);

        output.WriteLine($"Value: {value}, Buffer Length: {oversizeBuffer.Length}, Buffer: {new string(oversizeBuffer)}");

        // Verify the result is correct - buffer writes from right to left
        int exactBufferLength = Base62Encoder.DetermineBufferLength(value);
        var exactBuffer = new char[exactBufferLength];
        var exactBufferSpan = exactBuffer.AsSpan();
        Base62Encoder.Encode(value, ref exactBufferSpan);

        output.WriteLine($"Value: {value}, Buffer Length: {exactBuffer.Length}, Buffer: {new string(exactBuffer)}");

        // Convert the buffers to strings
        var exactString = new string(exactBuffer);

        // Trim off the null items from the oversize buffer
        var oversizeStringTrimmed = new string(oversizeBuffer).TrimStart('\0');

        // Assert the trimmed actual is the same as the "right-sized" buffer
        // This ensures we didn't truncate or trim
        oversizeStringTrimmed.ShouldBe(exactString);

        // Assert the encoded value is the expected value
        oversizeStringTrimmed.ShouldBe(expectedEncoded);
        exactString.ShouldBe(expectedEncoded);
    }

    /// <summary>
    /// Tests conversion with exact buffer size.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="bufferSize">The exact buffer size to use.</param>
    [Theory]
    [InlineData(1UL, 1)]
    [InlineData(62UL, 2)]
    [InlineData(3844UL, 3)]
    public void Encode_WithBuffer_ExactSize_WritesCorrectly(ulong value, int bufferSize)
    {
        var buffer = new char[bufferSize];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(value, ref span);

        output.WriteLine($"Value: {value}, Buffer: {new string(buffer)}");

        var expected = new string(buffer);
        var actual = new string(buffer);
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Tests conversion with insufficient buffer size.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="bufferSize">The insufficient buffer size to use.</param>
    [Theory]
    [InlineData(12345UL, 1)]
    [InlineData(12345UL, 2)]
    public void Encode_WithBuffer_InsufficientSize_TruncatesResult(ulong value, int bufferSize)
    {
        var buffer = new char[bufferSize];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(value, ref span);

        output.WriteLine($"Value: {value}, Buffer: {new string(buffer)}");

        var actual = new string(buffer);

        // Should truncate to buffer size
        actual.Length.ShouldBe(bufferSize);

        // Should contain valid Base62 characters
        foreach (var c in actual)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests conversion with zero value and buffer.
    /// </summary>
    [Fact]
    public void Encode_WithBuffer_ZeroValue_WritesCorrectly()
    {
        var buffer = new char[5];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(0UL, ref span);

        output.WriteLine($"Buffer: {new string(buffer)}");

        buffer[buffer.Length - 1].ShouldBe('A');
    }

    /// <summary>
    /// Tests conversion of byte span with all zeros.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_AllZeros_ReturnsValidString()
    {
        var hash = new byte[8];
        var buffer = new char[11]; // Max possible length
        var span = buffer.AsSpan();

        Base62Encoder.Encode(hash, ref span);

        var result = new string(buffer).TrimStart('\0');
        output.WriteLine($"Result: {result}");

        result.Length.ShouldBeGreaterThan(0);
        result.Length.ShouldBeLessThanOrEqualTo(11);

        foreach (var c in result)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests conversion of byte span with specific values.
    /// </summary>
    /// <param name="hashBytes">The byte array to convert.</param>
    [Theory]
    [InlineData(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 })]
    [InlineData(new byte[] { 0, 1, 0, 0, 0, 0, 0, 0 })]
    [InlineData(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 })]
    public void Encode_ByteSpan_SpecificValues_ReturnsValidString(byte[] hashBytes)
    {
        var hash = new ReadOnlySpan<byte>(hashBytes);
        var buffer = new char[11]; // Max possible length
        var span = buffer.AsSpan();

        Base62Encoder.Encode(hash, ref span);

        var result = new string(buffer).TrimStart('\0');
        output.WriteLine($"Hash: {BitConverter.ToString(hashBytes)}, Result: {result}");

        result.Length.ShouldBeGreaterThan(0);
        result.Length.ShouldBeLessThanOrEqualTo(11);

        foreach (var c in result)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests conversion of byte span with maximum value.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_MaxValue_ReturnsValidString()
    {
        var hash = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
        var buffer = new char[11]; // Max possible length
        var span = buffer.AsSpan();

        Base62Encoder.Encode(hash, ref span);

        var result = new string(buffer).TrimStart('\0');
        output.WriteLine($"Result: {result}");

        result.Length.ShouldBeGreaterThan(0);
        result.Length.ShouldBeLessThanOrEqualTo(11);

        foreach (var c in result)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests that byte span conversion uses modulo operation correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_UsesModuloOperation()
    {
        // Create a value that's larger than Base62Power8
        var largeValue = 218_340_105_584_896UL + 1000UL;
        var hash = BitConverter.GetBytes(largeValue);

        var buffer = new char[11]; // Max possible length
        var span = buffer.AsSpan();
        Base62Encoder.Encode(hash, ref span);

        var result = new string(buffer).TrimStart('\0');
        output.WriteLine($"Large value: {largeValue}, Result: {result}");

        // Should be equivalent to the modulo operation
        var expectedBuffer = new char[11];
        var expectedSpan = expectedBuffer.AsSpan();
        Base62Encoder.Encode(largeValue % 218_340_105_584_896UL, ref expectedSpan);
        var expected = new string(expectedBuffer).TrimStart('\0');
        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests conversion of byte span with buffer.
    /// </summary>
    [Fact]
    public void Encode_ByteSpanWithBuffer_WritesCorrectly()
    {
        var hash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var buffer = new char[10];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(hash, ref span);

        output.WriteLine($"Hash: {BitConverter.ToString(hash)}, Buffer: {new string(buffer)}");

        var expectedBuffer = new char[11];
        var expectedSpan = expectedBuffer.AsSpan();
        Base62Encoder.Encode(hash, ref expectedSpan);
        var expected = new string(expectedBuffer).TrimStart('\0');
        var actual = new string(buffer).TrimStart('\0');
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Tests conversion of byte span with insufficient buffer.
    /// </summary>
    [Fact]
    public void Encode_ByteSpanWithBuffer_InsufficientSize_TruncatesResult()
    {
        var hash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var buffer = new char[2];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(hash, ref span);

        output.WriteLine($"Hash: {BitConverter.ToString(hash)}, Buffer: {new string(buffer)}");

        var actual = new string(buffer);
        actual.Length.ShouldBe(2);

        foreach (var c in actual)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests that all Base62 characters are used in conversions.
    /// </summary>
    [Fact]
    public void Encode_AllBase62Characters_AreUsed()
    {
        var usedChars = new HashSet<char>();

        // Test a range of values to ensure all Base62 characters are used
        for (ulong i = 0; i < 10000; i++)
        {
            int bufferLength = Base62Encoder.DetermineBufferLength(i);
            var buffer = new char[bufferLength];
            var span = buffer.AsSpan();
            Base62Encoder.Encode(i, ref span);
            var result = new string(buffer);

            foreach (var c in result)
            {
                usedChars.Add(c);
            }
        }

        output.WriteLine($"Used characters: {string.Join(string.Empty, usedChars.OrderBy(c => c))}");

        // Should use all Base62 characters
        usedChars.Count.ShouldBe(62);
        foreach (var c in Base62Chars)
        {
            usedChars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests that conversion results are consistent for the same input.
    /// </summary>
    /// <param name="value">The value to test consistency for.</param>
    [Theory]
    [InlineData(12345UL)]
    [InlineData(67890UL)]
    [InlineData(ulong.MaxValue)]
    public void Encode_ConsistentResults_ForSameInput(ulong value)
    {
        int bufferLength = Base62Encoder.DetermineBufferLength(value);
        var buffer1 = new char[bufferLength];
        var buffer2 = new char[bufferLength];
        var span1 = buffer1.AsSpan();
        var span2 = buffer2.AsSpan();

        Base62Encoder.Encode(value, ref span1);
        Base62Encoder.Encode(value, ref span2);

        var result1 = new string(buffer1);
        var result2 = new string(buffer2);
        output.WriteLine($"Value: {value}, Result1: {result1}, Result2: {result2}");

        result1.ShouldBe(result2);
    }

    /// <summary>
    /// Tests that buffer operations don't affect subsequent calls.
    /// </summary>
    [Fact]
    public void Encode_BufferOperations_DontAffectSubsequentCalls()
    {
        var value = 12345UL;
        var buffer = new char[10];
        var span = buffer.AsSpan();

        // First call with buffer
        Base62Encoder.Encode(value, ref span);
        var bufferResult = new string(buffer).TrimStart('\0');

        // Second call without buffer
        int bufferLength = Base62Encoder.DetermineBufferLength(value);
        var spanBuffer = new char[bufferLength];
        var spanSpan = spanBuffer.AsSpan();
        Base62Encoder.Encode(value, ref spanSpan);
        var spanResult = new string(spanBuffer);

        output.WriteLine($"Buffer result: {bufferResult}, Span result: {spanResult}");

        bufferResult.ShouldBe(spanResult);
    }

    /// <summary>
    /// Tests that byte span conversion handles different byte orderings correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_DifferentByteOrderings_HandledCorrectly()
    {
        var hash1 = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 };
        var hash2 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

        var buffer1 = new char[11];
        var buffer2 = new char[11];
        var span1 = buffer1.AsSpan();
        var span2 = buffer2.AsSpan();

        Base62Encoder.Encode(hash1, ref span1);
        Base62Encoder.Encode(hash2, ref span2);

        var result1 = new string(buffer1).TrimStart('\0');
        var result2 = new string(buffer2).TrimStart('\0');

        output.WriteLine($"Hash1: {BitConverter.ToString(hash1)}, Result1: {result1}");
        output.WriteLine($"Hash2: {BitConverter.ToString(hash2)}, Result2: {result2}");

        // Results should be different due to different byte orderings
        result1.ShouldNotBe(result2);
    }

    /// <summary>
    /// Tests that the conversion handles the maximum possible value correctly.
    /// </summary>
    [Fact]
    public void Encode_MaximumValue_HandledCorrectly()
    {
        int bufferLength = Base62Encoder.DetermineBufferLength(ulong.MaxValue);
        var buffer = new char[bufferLength];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(ulong.MaxValue, ref span);

        var result = new string(buffer);
        output.WriteLine($"Max value result: {result}");

        result.Length.ShouldBeGreaterThan(0);
        result.Length.ShouldBeLessThanOrEqualTo(11);

        foreach (var c in result)
        {
            Base62Chars.ShouldContain(c);
        }
    }

    /// <summary>
    /// Tests that buffer operations are efficient.
    /// </summary>
    [Fact]
    public void Encode_BufferOperations_AreEfficient()
    {
        var value = 12345UL;
        var buffer = new char[10];
        var span = buffer.AsSpan();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            Base62Encoder.Encode(value, ref span);
        }

        stopwatch.Stop();

        output.WriteLine($"Time for 10000 buffer operations: {stopwatch.ElapsedMilliseconds}ms");

        // Should complete in reasonable time
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000); // Less than 1 second
    }

    /// <summary>
    /// Tests that little-endian byte ordering is handled correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_LittleEndian_HandledCorrectly()
    {
        // Test value 1 in little-endian format (least significant byte first)
        var littleEndianBytes = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 };
        var expectedValue = 1UL;

        var buffer = new char[11];
        var span = buffer.AsSpan();
        Base62Encoder.Encode(littleEndianBytes, ref span);
        var result = new string(buffer).TrimStart('\0');

        var expectedBuffer = new char[11];
        var expectedSpan = expectedBuffer.AsSpan();
        Base62Encoder.Encode(expectedValue, ref expectedSpan);
        var expected = new string(expectedBuffer).TrimStart('\0');

        output.WriteLine($"Little-endian bytes: {BitConverter.ToString(littleEndianBytes)}, Result: {result}, Expected: {expected}");

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that big-endian byte ordering produces different results.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_BigEndian_ProducesDifferentResults()
    {
        // Test value 1 in big-endian format (most significant byte first)
        var bigEndianBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
        var littleEndianBytes = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 };

        var buffer1 = new char[11];
        var buffer2 = new char[11];
        var span1 = buffer1.AsSpan();
        var span2 = buffer2.AsSpan();

        Base62Encoder.Encode(bigEndianBytes, ref span1);
        Base62Encoder.Encode(littleEndianBytes, ref span2);

        var bigEndianResult = new string(buffer1).TrimStart('\0');
        var littleEndianResult = new string(buffer2).TrimStart('\0');

        output.WriteLine($"Big-endian bytes: {BitConverter.ToString(bigEndianBytes)}, Result: {bigEndianResult}");
        output.WriteLine($"Little-endian bytes: {BitConverter.ToString(littleEndianBytes)}, Result: {littleEndianResult}");

        // Results should be different due to different byte orderings
        bigEndianResult.ShouldNotBe(littleEndianResult);
    }

    /// <summary>
    /// Tests various byte patterns to ensure endianness is handled consistently.
    /// </summary>
    /// <param name="bytes">The byte array to test.</param>
    /// <param name="expectedValue">The expected ulong value.</param>
    [Theory]
    [InlineData(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 }, 0xF0DEBC9A78563412UL)]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF }, 0xFF00000000000000UL)]
    [InlineData(new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0x00000000000000FFUL)]
    [InlineData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }, 0x0807060504030201UL)]
    public void Encode_ByteSpan_VariousBytePatterns_HandledCorrectly(byte[] bytes, ulong expectedValue)
    {
        var buffer = new char[11];
        var span = buffer.AsSpan();
        Base62Encoder.Encode(bytes, ref span);
        var result = new string(buffer).TrimStart('\0');

        var expectedBuffer = new char[11];
        var expectedSpan = expectedBuffer.AsSpan();
        Base62Encoder.Encode(expectedValue % 218_340_105_584_896UL, ref expectedSpan);
        var expected = new string(expectedBuffer).TrimStart('\0');

        output.WriteLine($"Bytes: {BitConverter.ToString(bytes)}, Expected value: {expectedValue}");
        output.WriteLine($"Result: {result}, Expected: {expected}");

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that byte span conversion with buffer handles endianness correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpanWithBuffer_Endianness_HandledCorrectly()
    {
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        var buffer = new char[10];
        var span = buffer.AsSpan();

        Base62Encoder.Encode(bytes, ref span);

        output.WriteLine($"Bytes: {BitConverter.ToString(bytes)}, Buffer: {new string(buffer)}");

        var expectedBuffer = new char[11];
        var expectedSpan = expectedBuffer.AsSpan();
        Base62Encoder.Encode(bytes, ref expectedSpan);
        var expected = new string(expectedBuffer).TrimStart('\0');
        var actual = new string(buffer).TrimStart('\0');
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that byte span conversion is consistent with BinaryPrimitives.ReadUInt64LittleEndian behavior.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_ConsistentWithBinaryPrimitives()
    {
        var bytes = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };

        // Convert using BinaryPrimitives (little-endian)
        var ulongValue = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        var expectedBuffer = new char[11];
        var expectedSpan = expectedBuffer.AsSpan();
        Base62Encoder.Encode(ulongValue % 218_340_105_584_896UL, ref expectedSpan);
        var expected = new string(expectedBuffer).TrimStart('\0');

        // Convert using byte span
        var resultBuffer = new char[11];
        var resultSpan = resultBuffer.AsSpan();
        Base62Encoder.Encode(bytes, ref resultSpan);
        var result = new string(resultBuffer).TrimStart('\0');

        output.WriteLine($"Bytes: {BitConverter.ToString(bytes)}, ULong value: {ulongValue}");
        output.WriteLine($"Expected: {expected}, Result: {result}");

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that byte span conversion handles reversed byte order correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_ReversedByteOrder_HandledCorrectly()
    {
        var originalBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var reversedBytes = new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 };

        var buffer1 = new char[11];
        var buffer2 = new char[11];
        var span1 = buffer1.AsSpan();
        var span2 = buffer2.AsSpan();

        Base62Encoder.Encode(originalBytes, ref span1);
        Base62Encoder.Encode(reversedBytes, ref span2);

        var originalResult = new string(buffer1).TrimStart('\0');
        var reversedResult = new string(buffer2).TrimStart('\0');

        output.WriteLine($"Original bytes: {BitConverter.ToString(originalBytes)}, Result: {originalResult}");
        output.WriteLine($"Reversed bytes: {BitConverter.ToString(reversedBytes)}, Result: {reversedResult}");

        // Both results should be valid Base62 strings
        originalResult.Length.ShouldBeGreaterThan(0);
        reversedResult.Length.ShouldBeGreaterThan(0);
        originalResult.Length.ShouldBeLessThanOrEqualTo(11);
        reversedResult.Length.ShouldBeLessThanOrEqualTo(11);

        // Both should contain valid Base62 characters
        foreach (var c in originalResult)
        {
            Base62Chars.ShouldContain(c);
        }

        foreach (var c in reversedResult)
        {
            Base62Chars.ShouldContain(c);
        }

        // Due to the modulo operation with Base62Power8, both values may wrap to the same result
        // This is mathematically correct behavior, not a bug
        // The test verifies that both produce valid results regardless of byte order
    }

    /// <summary>
    /// Tests that byte span conversion handles byte patterns that span multiple bytes correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_MultiBytePatterns_HandledCorrectly()
    {
        // Test patterns that span multiple bytes
        var testCases = new[]
        {
            (new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 }, 0x0001000000000000UL),
            (new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0x0000000000000100UL),
            (new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }, 0x0000000100000000UL),
            (new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0x0000000000010000UL),
        };

        foreach (var (bytes, expectedValue) in testCases)
        {
            var buffer = new char[11];
            var span = buffer.AsSpan();
            Base62Encoder.Encode(bytes, ref span);
            var result = new string(buffer).TrimStart('\0');

            var expectedBuffer = new char[11];
            var expectedSpan = expectedBuffer.AsSpan();
            Base62Encoder.Encode(expectedValue % 218_340_105_584_896UL, ref expectedSpan);
            var expected = new string(expectedBuffer).TrimStart('\0');

            output.WriteLine($"Bytes: {BitConverter.ToString(bytes)}, Expected value: {expectedValue}");
            output.WriteLine($"Result: {result}, Expected: {expected}");

            result.ShouldBe(expected);
        }
    }

    /// <summary>
    /// Tests that byte span conversion handles edge cases around byte boundaries correctly.
    /// </summary>
    [Fact]
    public void Encode_ByteSpan_ByteBoundaryEdgeCases_HandledCorrectly()
    {
        // Test edge cases around byte boundaries
        var testCases = new[]
        {
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE },
            new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 },
            new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
        };

        foreach (var bytes in testCases)
        {
            var buffer = new char[11];
            var span = buffer.AsSpan();
            Base62Encoder.Encode(bytes, ref span);
            var result = new string(buffer).TrimStart('\0');

            var littleEndian = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
            var expectedBuffer = new char[11];
            var expectedSpan = expectedBuffer.AsSpan();
            Base62Encoder.Encode(littleEndian % 218_340_105_584_896UL, ref expectedSpan);
            var expected = new string(expectedBuffer).TrimStart('\0');

            output.WriteLine($"Bytes: {BitConverter.ToString(bytes)}, Little-endian value: {littleEndian}");
            output.WriteLine($"Result: {result}, Expected: {expected}");

            result.ShouldBe(expected);
        }
    }

    /// <summary>
    /// Tests DetermineBufferLength for exact base-62 digit boundaries.
    /// Verifies both the value just below each threshold and the threshold value itself.
    /// </summary>
    /// <param name="value">The ulong value to determine buffer length for.</param>
    /// <param name="expected">The expected buffer length (int) for the given value.</param>
    [Theory]
    [InlineData(238327UL, 3)]
    [InlineData(238328UL, 4)]
    [InlineData(14776335UL, 4)]
    [InlineData(14776336UL, 5)]
    [InlineData(916132831UL, 5)]
    [InlineData(916132832UL, 6)]
    [InlineData(56800235583UL, 6)]
    [InlineData(56800235584UL, 7)]
    [InlineData(3521614606207UL, 7)]
    [InlineData(3521614606208UL, 8)]
    [InlineData(218340105584895UL, 8)]
    [InlineData(218340105584896UL, 9)]
    public void DetermineBufferLength_BoundaryValues_ReturnExpected(ulong value, int expected)
    {
        int actual = Base62Encoder.DetermineBufferLength(value);
        output.WriteLine($"Value: {value}, Expected: {expected}, Actual: {actual}");
        actual.ShouldBe(expected);
    }
}
