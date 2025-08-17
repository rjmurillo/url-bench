using Logic;
using Logic.Repositories;
using Logic.Shorteners;
using Xunit;

namespace UrlConverterPermutations;

/// <summary>
/// Tests for the refactored UrlManager that uses simplified collision handling.
/// </summary>
public class UrlManagerTests
{
    [Fact]
    public void ShortenAndSave_WithSuccessfulOperation_ReturnsShortUrl()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        var longUrl = "https://example.com/very/long/url/that/needs/shortening";

        // Act
        var shortUrl = urlManager.ShortenAndSave(longUrl);

        // Assert
        Assert.NotNull(shortUrl);
        Assert.NotEmpty(shortUrl);
        Assert.NotEqual(longUrl, shortUrl);

        // Verify the mapping was saved
        var retrievedUrl = urlManager.GetOriginalUrl(shortUrl);
        Assert.Equal(longUrl, retrievedUrl);
    }

    [Fact]
    public void ShortenAndSave_WithSameUrlTwice_ReturnsSameShortUrl()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        var longUrl = "https://example.com/very/long/url/that/needs/shortening";

        // Act
        var shortUrl1 = urlManager.ShortenAndSave(longUrl);
        var shortUrl2 = urlManager.ShortenAndSave(longUrl);

        // Assert
        Assert.Equal(shortUrl1, shortUrl2);
    }

    [Fact]
    public void ShortenAndSave_WithDifferentUrls_ReturnsDifferentShortUrls()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        var longUrl1 = "https://example.com/very/long/url/that/needs/shortening";
        var longUrl2 = "https://example.com/another/very/long/url/that/needs/shortening";

        // Act
        var shortUrl1 = urlManager.ShortenAndSave(longUrl1);
        var shortUrl2 = urlManager.ShortenAndSave(longUrl2);

        // Assert
        Assert.NotEqual(shortUrl1, shortUrl2);
    }

    [Fact]
    public void ShortenAndSave_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => urlManager.ShortenAndSave(null!));
    }

    [Fact]
    public void ShortenAndSave_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => urlManager.ShortenAndSave(string.Empty));
    }

    [Fact]
    public void ShortenAndSave_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => urlManager.ShortenAndSave("   "));
    }

    [Fact]
    public void GetOriginalUrl_WithExistingUrl_ReturnsOriginalUrl()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        var longUrl = "https://example.com/very/long/url/that/needs/shortening";
        var shortUrl = urlManager.ShortenAndSave(longUrl);

        // Act
        var retrievedUrl = urlManager.GetOriginalUrl(shortUrl);

        // Assert
        Assert.Equal(longUrl, retrievedUrl);
    }

    [Fact]
    public void GetOriginalUrl_WithNonExistentUrl_ReturnsEmptyString()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act
        var retrievedUrl = urlManager.GetOriginalUrl("nonexistent");

        // Assert
        Assert.Equal(string.Empty, retrievedUrl);
    }

    [Fact]
    public void GetOriginalUrl_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => urlManager.GetOriginalUrl(null!));
    }

    [Fact]
    public void GetOriginalUrl_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => urlManager.GetOriginalUrl(string.Empty));
    }

    [Fact]
    public void GetOriginalUrl_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Arrange
        var shortener = new Murmur3HashUrlShortener();
        var repository = new ConcurrentUrlRepository();
        var urlManager = new UrlManager(shortener, repository);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => urlManager.GetOriginalUrl("   "));
    }
}
