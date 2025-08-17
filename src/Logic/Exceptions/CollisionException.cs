using System.Runtime.Serialization;

namespace Logic.Exceptions;

/// <summary>
/// Exception thrown when a collision is detected during URL shortening operations.
/// This exception indicates that the generated short URL already exists in the repository
/// and is associated with a different original URL.
/// </summary>
[Serializable]
public class CollisionException : Exception
{
    /// <summary>
    /// Gets the short URL that caused the collision.
    /// </summary>
    public string ShortUrl { get; }

    /// <summary>
    /// Gets the original URL that was being shortened.
    /// </summary>
    public string OriginalUrl { get; }

    /// <summary>
    /// Gets the existing URL that was already associated with the short URL.
    /// </summary>
    public string? ExistingUrl { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionException"/> class.
    /// </summary>
    public CollisionException()
    {
        ShortUrl = string.Empty;
        OriginalUrl = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CollisionException(string message) : base(message)
    {
        ShortUrl = string.Empty;
        OriginalUrl = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CollisionException(string message, Exception innerException) : base(message, innerException)
    {
        ShortUrl = string.Empty;
        OriginalUrl = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionException"/> class with collision details.
    /// </summary>
    /// <param name="shortUrl">The short URL that caused the collision.</param>
    /// <param name="originalUrl">The original URL that was being shortened.</param>
    /// <param name="existingUrl">The existing URL that was already associated with the short URL.</param>
    public CollisionException(string shortUrl, string originalUrl, string? existingUrl = null)
        : base($"Collision detected for short URL '{shortUrl}'. Original URL: '{originalUrl}', Existing URL: '{existingUrl ?? "unknown"}'")
    {
        ShortUrl = shortUrl ?? string.Empty;
        OriginalUrl = originalUrl ?? string.Empty;
        ExistingUrl = existingUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionException"/> class with collision details and inner exception.
    /// </summary>
    /// <param name="shortUrl">The short URL that caused the collision.</param>
    /// <param name="originalUrl">The original URL that was being shortened.</param>
    /// <param name="existingUrl">The existing URL that was already associated with the short URL.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CollisionException(string shortUrl, string originalUrl, string? existingUrl, Exception innerException)
        : base($"Collision detected for short URL '{shortUrl}'. Original URL: '{originalUrl}', Existing URL: '{existingUrl ?? "unknown"}'", innerException)
    {
        ShortUrl = shortUrl ?? string.Empty;
        OriginalUrl = originalUrl ?? string.Empty;
        ExistingUrl = existingUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected CollisionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ShortUrl = info.GetString(nameof(ShortUrl)) ?? string.Empty;
        OriginalUrl = info.GetString(nameof(OriginalUrl)) ?? string.Empty;
        ExistingUrl = info.GetString(nameof(ExistingUrl));
    }

    /// <summary>
    /// Sets the <see cref="SerializationInfo"/> with information about the exception.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        info.AddValue(nameof(ShortUrl), ShortUrl);
        info.AddValue(nameof(OriginalUrl), OriginalUrl);
        info.AddValue(nameof(ExistingUrl), ExistingUrl);

        base.GetObjectData(info, context);
    }
}
