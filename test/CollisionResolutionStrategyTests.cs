using Logic.CollisionResolution;
using Logic.Contracts;

namespace UrlConverterPermutations;

public class CollisionResolutionStrategyTests
{
    [Fact]
    public void NullCollisionResolutionStrategy_WithAttemptZero_ReturnsBaseShortUrl()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl);

        // Assert
        Assert.Equal(baseShortUrl, result);
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithAttemptOne_ReturnsBaseShortUrl()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 1, longUrl);

        // Assert
        Assert.Equal(baseShortUrl, result);
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithAttemptTwo_ReturnsBaseShortUrl()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 2, longUrl);

        // Assert
        Assert.Equal(baseShortUrl, result);
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithNegativeAttempt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, -1, longUrl));
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithNullBaseShortUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        string? baseShortUrl = null;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl!, 0, longUrl));
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithEmptyBaseShortUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = string.Empty;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithNullLongUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        string? longUrl = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl!));
    }

    [Fact]
    public void NullCollisionResolutionStrategy_WithEmptyLongUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new NullCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithAttemptZero_ReturnsBaseShortUrl()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl);

        // Assert
        Assert.Equal(baseShortUrl, result);
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithAttemptOne_AppendsSuffix()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 1, longUrl);

        // Assert
        Assert.Equal("abc123_1", result);
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithAttemptTwo_AppendsSuffix()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 2, longUrl);

        // Assert
        Assert.Equal("abc123_2", result);
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithNegativeAttempt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, -1, longUrl));
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithNullBaseShortUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        string? baseShortUrl = null;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl!, 0, longUrl));
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithEmptyBaseShortUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = string.Empty;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithNullLongUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        string? longUrl = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl!));
    }

    [Fact]
    public void SuffixCollisionResolutionStrategy_WithEmptyLongUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new SuffixCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithAttemptZero_ReturnsBaseShortUrl()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl);

        // Assert
        Assert.Equal(baseShortUrl, result);
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithAttemptOne_AppendsUuidSuffix()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 1, longUrl);

        // Assert
        Assert.StartsWith(baseShortUrl + "_", result);
        Assert.Contains("_", result);
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithAttemptTwo_AppendsDifferentUuidSuffix()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result1 = strategy.GenerateUniqueShortUrl(baseShortUrl, 1, longUrl);
        var result2 = strategy.GenerateUniqueShortUrl(baseShortUrl, 2, longUrl);

        // Assert
        Assert.StartsWith(baseShortUrl + "_", result1);
        Assert.StartsWith(baseShortUrl + "_", result2);
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithNegativeAttempt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, -1, longUrl));
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithNullBaseShortUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        string? baseShortUrl = null;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl!, 0, longUrl));
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithEmptyBaseShortUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = string.Empty;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithNullLongUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        string? longUrl = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl!));
    }

    [Fact]
    public void UuidCollisionResolutionStrategy_WithEmptyLongUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new UuidCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithAttemptZero_ReturnsBaseShortUrl()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl);

        // Assert
        Assert.Equal(baseShortUrl, result);
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithAttemptOne_AppendsTimestampSuffix()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result = strategy.GenerateUniqueShortUrl(baseShortUrl, 1, longUrl);

        // Assert
        Assert.StartsWith(baseShortUrl + "_", result);
        Assert.Contains("_", result);
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithAttemptTwo_AppendsDifferentTimestampSuffix()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act
        var result1 = strategy.GenerateUniqueShortUrl(baseShortUrl, 1, longUrl);
        var result2 = strategy.GenerateUniqueShortUrl(baseShortUrl, 2, longUrl);

        // Assert
        Assert.StartsWith(baseShortUrl + "_", result1);
        Assert.StartsWith(baseShortUrl + "_", result2);
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithNegativeAttempt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, -1, longUrl));
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithNullBaseShortUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        string? baseShortUrl = null;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl!, 0, longUrl));
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithEmptyBaseShortUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = string.Empty;
        var longUrl = "https://example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithNullLongUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        string? longUrl = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl!));
    }

    [Fact]
    public void TimestampCollisionResolutionStrategy_WithEmptyLongUrl_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new TimestampCollisionResolutionStrategy();
        var baseShortUrl = "abc123";
        var longUrl = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strategy.GenerateUniqueShortUrl(baseShortUrl, 0, longUrl));
    }
}
