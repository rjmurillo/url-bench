using System.Buffers;
using System.Text;
using Logic;

namespace UrlConverterPermutations;

/// <summary>
/// Test class for the Convert utility class.
/// </summary>
/// <param name="output">The test output helper for writing test information.</param>
public class ConvertTests(ITestOutputHelper output)
{
    private const ulong Base62Power8 = 218_340_105_584_896UL;
    private static readonly string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Tests conversion of zero value.
    /// </summary>
    [Fact]
    public void AsBase62String_ZeroValue_ReturnsSingleCharacter()
    {
        var result = Logic.Convert.AsBase62String(0UL);

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
    public void AsBase62String_SingleDigitValues_ReturnsCorrectCharacter(ulong value, string expected)
    {
        var result = Logic.Convert.AsBase62String(value);

        output.WriteLine($"Value: {value}, Result: {result}, Expected: {expected}");

        result.ToString().ShouldBe(expected);
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
    public void AsBase62String_TwoDigitValues_ReturnsCorrectString(ulong value, string expected)
    {
        var result = Logic.Convert.AsBase62String(value);

        output.WriteLine($"Value: {value}, Result: {result}, Expected: {expected}");

        result.ToString().ShouldBe(expected);
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
    public void AsBase62String_LargeValues_ReturnsValidBase62String(ulong value)
    {
        var result = Logic.Convert.AsBase62String(value);

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
    /// Tests that the result is a valid ReadOnlySpan.
    /// </summary>
    [Fact]
    public void AsBase62String_ReturnsValidReadOnlySpan()
    {
        var result = Logic.Convert.AsBase62String(12345UL);

        result.IsEmpty.ShouldBeFalse();
    }

    /// <summary>
    /// Tests conversion with sufficient buffer size.
    /// </summary>
    [Fact]
    public void AsBase62String_WithBuffer_SufficientSize_WritesCorrectly()
    {
        var value = 12345UL;
        var buffer = new char[10];
        var span = buffer.AsSpan();

        Logic.Convert.AsBase62String(value, ref span);

        output.WriteLine($"Value: {value}, Buffer: {new string(buffer)}");

        // Verify the result is correct - buffer writes from right to left
        var expected = Logic.Convert.AsBase62String(value);
        var actual = new string(buffer).TrimStart('\0');
        actual.ShouldBe(expected.ToString());
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
    public void AsBase62String_WithBuffer_ExactSize_WritesCorrectly(ulong value, int bufferSize)
    {
        var buffer = new char[bufferSize];
        var span = buffer.AsSpan();

        Logic.Convert.AsBase62String(value, ref span);

        output.WriteLine($"Value: {value}, Buffer: {new string(buffer)}");

        var expected = Logic.Convert.AsBase62String(value);
        var actual = new string(buffer);
        actual.ShouldBe(expected.ToString());
    }

    /// <summary>
    /// Tests conversion with insufficient buffer size.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="bufferSize">The insufficient buffer size to use.</param>
    [Theory]
    [InlineData(12345UL, 1)]
    [InlineData(12345UL, 2)]
    public void AsBase62String_WithBuffer_InsufficientSize_TruncatesResult(ulong value, int bufferSize)
    {
        var buffer = new char[bufferSize];
        var span = buffer.AsSpan();

        Logic.Convert.AsBase62String(value, ref span);

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
    public void AsBase62String_WithBuffer_ZeroValue_WritesCorrectly()
    {
        var buffer = new char[5];
        var span = buffer.AsSpan();

        Logic.Convert.AsBase62String(0UL, ref span);

        output.WriteLine($"Buffer: {new string(buffer)}");

        buffer[buffer.Length - 1].ShouldBe('A');
    }

    /// <summary>
    /// Tests conversion of byte span with all zeros.
    /// </summary>
    [Fact]
    public void AsBase62String_ByteSpan_AllZeros_ReturnsValidString()
    {
        var hash = new byte[8];

        var result = Logic.Convert.AsBase62String(hash);

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
    public void AsBase62String_ByteSpan_SpecificValues_ReturnsValidString(byte[] hashBytes)
    {
        var hash = new Span<byte>(hashBytes);

        var result = Logic.Convert.AsBase62String(hash);

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
    public void AsBase62String_ByteSpan_MaxValue_ReturnsValidString()
    {
        var hash = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };

        var result = Logic.Convert.AsBase62String(hash);

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
    public void AsBase62String_ByteSpan_UsesModuloOperation()
    {
        // Create a value that's larger than Base62Power8
        var largeValue = Base62Power8 + 1000UL;
        var hash = BitConverter.GetBytes(largeValue);

        var result = Logic.Convert.AsBase62String(hash);

        output.WriteLine($"Large value: {largeValue}, Result: {result}");

        // Should be equivalent to the modulo operation
        var expected = Logic.Convert.AsBase62String(largeValue % Base62Power8);
        result.ToString().ShouldBe(expected.ToString());
    }

    /// <summary>
    /// Tests conversion of byte span with buffer.
    /// </summary>
    [Fact]
    public void AsBase62String_ByteSpanWithBuffer_WritesCorrectly()
    {
        var hash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var buffer = new char[10];
        var span = buffer.AsSpan();

        Logic.Convert.AsBase62String(hash, ref span);

        output.WriteLine($"Hash: {BitConverter.ToString(hash)}, Buffer: {new string(buffer)}");

        var expected = Logic.Convert.AsBase62String(hash);
        var actual = new string(buffer).TrimStart('\0');
        actual.ShouldBe(expected.ToString());
    }

    /// <summary>
    /// Tests conversion of byte span with insufficient buffer.
    /// </summary>
    [Fact]
    public void AsBase62String_ByteSpanWithBuffer_InsufficientSize_TruncatesResult()
    {
        var hash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var buffer = new char[2];
        var span = buffer.AsSpan();

        Logic.Convert.AsBase62String(hash, ref span);

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
    public void AsBase62String_AllBase62Characters_AreUsed()
    {
        var usedChars = new HashSet<char>();

        // Test a range of values to ensure all Base62 characters are used
        for (ulong i = 0; i < 10000; i++)
        {
            var result = Logic.Convert.AsBase62String(i);
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
    public void AsBase62String_ConsistentResults_ForSameInput(ulong value)
    {
        var result1 = Logic.Convert.AsBase62String(value);
        var result2 = Logic.Convert.AsBase62String(value);

        output.WriteLine($"Value: {value}, Result1: {result1}, Result2: {result2}");

        result1.ToString().ShouldBe(result2.ToString());
    }

    /// <summary>
    /// Tests that buffer operations don't affect subsequent calls.
    /// </summary>
    [Fact]
    public void AsBase62String_BufferOperations_DontAffectSubsequentCalls()
    {
        var value = 12345UL;
        var buffer = new char[10];
        var span = buffer.AsSpan();

        // First call with buffer
        Logic.Convert.AsBase62String(value, ref span);
        var bufferResult = new string(buffer).TrimStart('\0');

        // Second call without buffer
        var spanResult = Logic.Convert.AsBase62String(value);

        output.WriteLine($"Buffer result: {bufferResult}, Span result: {spanResult}");

        bufferResult.ShouldBe(spanResult.ToString());
    }

    /// <summary>
    /// Tests that byte span conversion handles different byte orderings correctly.
    /// </summary>
    [Fact]
    public void AsBase62String_ByteSpan_DifferentByteOrderings_HandledCorrectly()
    {
        var hash1 = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 };
        var hash2 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

        var result1 = Logic.Convert.AsBase62String(hash1);
        var result2 = Logic.Convert.AsBase62String(hash2);

        output.WriteLine($"Hash1: {BitConverter.ToString(hash1)}, Result1: {result1}");
        output.WriteLine($"Hash2: {BitConverter.ToString(hash2)}, Result2: {result2}");

        // Results should be different due to different byte orderings
        result1.ToString().ShouldNotBe(result2.ToString());
    }

    /// <summary>
    /// Tests that the conversion handles the maximum possible value correctly.
    /// </summary>
    [Fact]
    public void AsBase62String_MaximumValue_HandledCorrectly()
    {
        var result = Logic.Convert.AsBase62String(ulong.MaxValue);

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
    public void AsBase62String_BufferOperations_AreEfficient()
    {
        var value = 12345UL;
        var buffer = new char[10];
        var span = buffer.AsSpan();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            Logic.Convert.AsBase62String(value, ref span);
        }

        stopwatch.Stop();

        output.WriteLine($"Time for 10000 buffer operations: {stopwatch.ElapsedMilliseconds}ms");

        // Should complete in reasonable time
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000); // Less than 1 second
    }
}
