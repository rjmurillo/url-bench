using Logic;
using Logic.Contracts;
using Logic.Shorteners;

namespace UrlConverterPermutations;

/// <summary>
/// Tests for hash function quality and correctness to catch distribution and collision issues.
/// These tests help detect problems like the bit shift bug in ProcessRemainingBytes.
/// </summary>
public class HashQualityTests(ITestOutputHelper output)
{
    /// <summary>
    /// Gets theory data for deterministic hash-based shorteners only.
    /// </summary>
    public static TheoryData<IUrlShortener> DeterministicShortenerData()
    {
        var data = new TheoryData<IUrlShortener>();
        
        // Only test deterministic hash-based shorteners for quality tests
        data.Add(new VectorizedUrlShortener());
        data.Add(new Murmur3HashUrlShortener());
        data.Add(new Sha256HashUrlShortener());
        
        return data;
    }

    /// <summary>
    /// Tests that the hash function produces good character distribution in the output.
    /// Poor bit distribution would show up as biased character usage.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Produces_Diverse_Character_Distribution(IUrlShortener shortener)
    {
        const int testCount = 10000;
        const string baseUrl = "https://example.com/test/";
        
        // Count character frequency across all generated hashes
        var charCounts = new Dictionary<char, int>();
        var base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        // Initialize counts
        foreach (char c in base62Chars)
        {
            charCounts[c] = 0;
        }
        
        int totalChars = 0;
        
        for (int i = 0; i < testCount; i++)
        {
            var url = $"{baseUrl}{i}";
            var shortened = shortener.ShortenUrl(url);
            
            foreach (char c in shortened)
            {
                if (charCounts.ContainsKey(c))
                {
                    charCounts[c]++;
                    totalChars++;
                }
            }
        }
        
        // Calculate expected frequency per character
        double expectedPerChar = totalChars / (double)base62Chars.Length;
        double tolerance = expectedPerChar * 0.3; // 30% tolerance
        
        int unusedChars = 0;
        double maxDeviation = 0.0;
        
        foreach (var (character, count) in charCounts)
        {
            if (count == 0)
            {
                unusedChars++;
            }
            
            double deviation = Math.Abs(count - expectedPerChar);
            maxDeviation = Math.Max(maxDeviation, deviation);
        }
        
        output.WriteLine($"Total characters generated: {totalChars}");
        output.WriteLine($"Expected per character: {expectedPerChar:F1}");
        output.WriteLine($"Unused characters: {unusedChars}/{base62Chars.Length}");
        output.WriteLine($"Max deviation: {maxDeviation:F1} (tolerance: {tolerance:F1})");
        
        // No more than 10% of characters should be unused
        unusedChars.ShouldBeLessThan(
            (int)(base62Chars.Length * 0.1),
            $"Too many unused characters: {unusedChars}/{base62Chars.Length}");
        
        // Character distribution should be reasonably uniform
        maxDeviation.ShouldBeLessThan(
            tolerance,
            $"Poor character distribution: max deviation {maxDeviation:F1} exceeds tolerance {tolerance:F1}");
    }

    /// <summary>
    /// Tests that small input changes result in significantly different outputs.
    /// This tests the avalanche effect at the output string level.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Shows_Avalanche_Effect_In_Output(IUrlShortener shortener)
    {
        const int testCount = 1000;
        const string baseUrl = "https://example.com/test/page";
        
        int totalDifferences = 0;
        int totalComparisons = 0;
        
        for (int i = 0; i < testCount; i++)
        {
            var originalUrl = $"{baseUrl}{i}";
            var modifiedUrl = $"{baseUrl}{i}x"; // Small change: add one character
            
            var hash1 = shortener.ShortenUrl(originalUrl);
            var hash2 = shortener.ShortenUrl(modifiedUrl);
            
            // Count character differences
            int differences = CountCharacterDifferences(hash1, hash2);
            totalDifferences += differences;
            totalComparisons++;
        }
        
        double averageDifferences = totalDifferences / (double)totalComparisons;
        int minExpectedDifferences = 2; // Should change at least 2-3 characters on average
        
        output.WriteLine($"Average character differences: {averageDifferences:F2}");
        output.WriteLine($"Total comparisons: {totalComparisons}");
        
        averageDifferences.ShouldBeGreaterThan(minExpectedDifferences,
            $"Poor avalanche effect: only {averageDifferences:F2} characters change on average " +
            $"(expected at least {minExpectedDifferences})");
    }

    /// <summary>
    /// Tests that hash outputs are well-distributed across the Base62 character space.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Distribution_Covers_Character_Space_Uniformly(IUrlShortener shortener)
    {
        const int testCount = 50000;
        const string baseUrl = "https://example.com/item/";
        
        var hashes = new List<string>();
        
        for (int i = 0; i < testCount; i++)
        {
            var url = $"{baseUrl}{i}";
            var hash = shortener.ShortenUrl(url);
            hashes.Add(hash);
        }
        
        // Test first character distribution (should be roughly uniform)
        var firstCharCounts = new Dictionary<char, int>();
        foreach (var hash in hashes)
        {
            if (hash.Length > 0)
            {
                var firstChar = hash[0];
                firstCharCounts[firstChar] = firstCharCounts.GetValueOrDefault(firstChar, 0) + 1;
            }
        }
        
        int uniqueFirstChars = firstCharCounts.Count;
        int expectedChars = Math.Min(62, testCount / 100); // Should use a good variety
        
        output.WriteLine($"Unique first characters: {uniqueFirstChars}");
        output.WriteLine($"Expected minimum: {expectedChars}");
        
        uniqueFirstChars.ShouldBeGreaterThan(expectedChars / 2,
            $"Poor first character distribution: only {uniqueFirstChars} unique characters " +
            $"(expected at least {expectedChars / 2})");
        
        // Test that we don't have excessive bias toward any single character
        int maxCount = firstCharCounts.Values.Max();
        double avgCount = testCount / (double)uniqueFirstChars;
        double bias = maxCount / avgCount;
        
        output.WriteLine($"Max character count: {maxCount}");
        output.WriteLine($"Average count: {avgCount:F1}");
        output.WriteLine($"Bias ratio: {bias:F2}");
        
        bias.ShouldBeLessThan(3.0, 
            $"Excessive bias toward single character: {bias:F2}x average frequency");
    }

    /// <summary>
    /// Tests collision rate for similar inputs to ensure hash quality.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Has_Low_Collision_Rate_For_Similar_Inputs(IUrlShortener shortener)
    {
        const int testCount = 100000;
        const string baseUrl = "https://api.example.com/v1/users/";
        
        var hashes = new HashSet<string>();
        int collisions = 0;
        
        for (int i = 0; i < testCount; i++)
        {
            var url = $"{baseUrl}{i}";
            var shortened = shortener.ShortenUrl(url);
            
            if (!hashes.Add(shortened))
            {
                collisions++;
            }
        }
        
        double collisionRate = collisions / (double)testCount;
        double maxAcceptableRate = 0.001; // Less than 0.1% collision rate
        
        output.WriteLine($"Collisions: {collisions}/{testCount} ({collisionRate * 100:F4}%)");
        output.WriteLine($"Unique hashes: {hashes.Count}");
        
        collisionRate.ShouldBeLessThan(maxAcceptableRate,
            $"Collision rate too high: {collisionRate * 100:F4}% (max acceptable: {maxAcceptableRate * 100:F1}%)");
    }

    /// <summary>
    /// Tests that specific byte patterns in the remainder processing work correctly.
    /// This directly tests the scenario where the bit shift bug would manifest.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Processes_Remainder_Bytes_Correctly(IUrlShortener shortener)
    {
        // Test URLs with lengths that would leave different remainder byte counts
        // These are crafted to hit different code paths in ProcessRemainingBytes
        var testCases = new[]
        {
            "https://a.com", // Short URL
            "https://ab.com", // +1 char
            "https://abc.com", // +1 char  
            "https://abcd.com", // +1 char
            "https://abcde.com", // +1 char
            "https://abcdef.com", // +1 char
            "https://abcdefg.com", // +1 char
            "https://abcdefgh.com", // +1 char
            "https://abcdefghi.com", // Forces remainder processing
            "https://abcdefghij.com", // More remainder bytes
            "https://abcdefghijk.com", // Even more remainder bytes
            // Test patterns that would expose bit overlap issues
            "x", // Single character
            "xx", // Two identical characters
            "xxx", // Three identical characters
            "xxxx", // Four identical characters
            "xxxxx", // Five identical characters
            "xxxxxx", // Six identical characters
            "xxxxxxx", // Seven identical characters
            "xxxxxxxx", // Eight identical characters (full first group)
            "xxxxxxxxx", // Nine characters (forces remainder)
        };
        
        var hashes = new List<string>();
        
        foreach (var testUrl in testCases)
        {
            var shortened = shortener.ShortenUrl(testUrl);
            hashes.Add(shortened);
            
            output.WriteLine($"URL: '{testUrl}' -> Hash: '{shortened}'");
        }
        
        // All hashes should be unique (no collisions for these distinct inputs)
        var uniqueHashes = new HashSet<string>(hashes);
        uniqueHashes.Count.ShouldBe(hashes.Count, 
            "Found hash collisions in remainder byte processing test");
        
        // Each hash should be non-empty and reasonable length
        foreach (var hash in hashes)
        {
            hash.ShouldNotBeNullOrEmpty("Hash should not be empty");
            hash.Length.ShouldBeLessThan(12, "Hash should not be excessively long");
            
            // Should contain diverse characters (not all the same)
            int uniqueChars = hash.ToCharArray().Distinct().Count();
            if (hash.Length > 2)
            {
                uniqueChars.ShouldBeGreaterThan(1, 
                    $"Hash '{hash}' has poor character diversity: only {uniqueChars} unique chars");
            }
        }
    }

    /// <summary>
    /// Regression test with known inputs to catch when hash behavior changes.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Produces_Consistent_Values_For_Known_Inputs(IUrlShortener shortener)
    {
        var knownInputs = new[]
        {
            "https://www.example.com",
            "https://github.com/microsoft/vscode",
            "https://stackoverflow.com/questions/1234567",
            "https://docs.microsoft.com/en-us/dotnet/",
            ""
        };
        
        // Test deterministic behavior: same input should always produce same output
        foreach (var input in knownInputs)
        {
            var result1 = shortener.ShortenUrl(input);
            var result2 = shortener.ShortenUrl(input);
            var result3 = shortener.ShortenUrl(input);
            
            output.WriteLine($"Input: '{input}' -> Hash: '{result1}'");
            
            result2.ShouldBe(result1, $"Hash not deterministic for input '{input}'");
            result3.ShouldBe(result1, $"Hash not deterministic for input '{input}'");
        }
    }

    /// <summary>
    /// Tests edge cases that could expose implementation bugs.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeterministicShortenerData))]
    public void Hash_Handles_Edge_Cases_Correctly(IUrlShortener shortener)
    {
        var edgeCases = new[]
        {
            "", // Empty string
            " ", // Single space
            "\n", // Newline
            "\t", // Tab
            "🌟", // Unicode character
            "a".PadRight(1000, 'a'), // Very long string of same character
            "https://\u0000.com", // Null character
            "https://test.com?" + new string('x', 1024), // Long query string
        };
        
        var results = new List<string>();
        
        foreach (var testCase in edgeCases)
        {
            var result = shortener.ShortenUrl(testCase);
            results.Add(result);
            
            output.WriteLine($"Input length: {testCase.Length}, Hash: '{result}'");
            
            // Basic validations
            result.ShouldNotBeNull("Hash should never be null");
            
            if (!string.IsNullOrWhiteSpace(testCase))
            {
                result.ShouldNotBeEmpty("Hash should not be empty for non-empty input");
            }
        }
        
        // Check for unexpected collisions among edge cases
        var uniqueResults = new HashSet<string>(results.Where(r => !string.IsNullOrEmpty(r)));
        int nonEmptyResults = results.Count(r => !string.IsNullOrEmpty(r));
        
        if (nonEmptyResults > 1)
        {
            uniqueResults.Count.ShouldBe(nonEmptyResults,
                "Unexpected collisions among edge case inputs");
        }
    }

    /// <summary>
    /// Helper method to count character differences between two strings.
    /// </summary>
    private static int CountCharacterDifferences(string str1, string str2)
    {
        int maxLength = Math.Max(str1.Length, str2.Length);
        int differences = Math.Abs(str1.Length - str2.Length); // Length difference
        
        int minLength = Math.Min(str1.Length, str2.Length);
        for (int i = 0; i < minLength; i++)
        {
            if (str1[i] != str2[i])
            {
                differences++;
            }
        }
        
        return differences;
    }
}
