using System.Runtime.Serialization;

namespace Logic.Exceptions;

/// <summary>
/// Exception thrown when URL shortening operations fail.
/// This exception indicates that a URL shortening operation has failed
/// due to algorithmic issues, platform limitations, or other shortener-specific errors.
/// </summary>
[Serializable]
public class ShortenerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerException"/> class.
    /// </summary>
    public ShortenerException()
    {
        OriginalUrl = string.Empty;
        ShortenerType = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ShortenerException(string message)
        : base(message)
    {
        OriginalUrl = string.Empty;
        ShortenerType = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ShortenerException(string message, Exception innerException)
        : base(message, innerException)
    {
        OriginalUrl = string.Empty;
        ShortenerType = string.Empty;
    }

    /// <summary>
    /// Gets the original URL that was being shortened when the exception occurred.
    /// </summary>
    public string OriginalUrl { get; }

    /// <summary>
    /// Gets the shortener type that was being used.
    /// </summary>
    public string ShortenerType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerException"/> class with shortening details.
    /// </summary>
    /// <param name="originalUrl">The original URL that was being shortened.</param>
    /// <param name="shortenerType">The type of shortener that was being used.</param>
    /// <param name="message">The error message.</param>
    public ShortenerException(string originalUrl, string shortenerType, string message)
        : base($"URL shortening failed for '{originalUrl}' using {shortenerType}: {message}")
    {
        OriginalUrl = originalUrl ?? string.Empty;
        ShortenerType = shortenerType ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerException"/> class with shortening details and inner exception.
    /// </summary>
    /// <param name="originalUrl">The original URL that was being shortened.</param>
    /// <param name="shortenerType">The type of shortener that was being used.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ShortenerException(string originalUrl, string shortenerType, string message, Exception innerException)
        : base($"URL shortening failed for '{originalUrl}' using {shortenerType}: {message}", innerException)
    {
        OriginalUrl = originalUrl ?? string.Empty;
        ShortenerType = shortenerType ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
    protected ShortenerException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        OriginalUrl = info.GetString(nameof(OriginalUrl)) ?? string.Empty;
        ShortenerType = info.GetString(nameof(ShortenerType)) ?? string.Empty;
    }

    /// <summary>
    /// Sets the <see cref="SerializationInfo"/> with information about the exception.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        info.AddValue(nameof(OriginalUrl), OriginalUrl);
        info.AddValue(nameof(ShortenerType), ShortenerType);

        base.GetObjectData(info, context);
    }
}
