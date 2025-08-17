using System.Runtime.Serialization;

namespace Logic.Exceptions;

/// <summary>
/// Exception thrown when repository operations fail.
/// This exception indicates that a repository operation (save, retrieve, etc.) has failed
/// due to underlying storage issues, connectivity problems, or other repository-specific errors.
/// </summary>
[Serializable]
public class RepositoryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class.
    /// </summary>
    public RepositoryException()
    {
        Operation = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RepositoryException(string message)
        : base(message)
    {
        Operation = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RepositoryException(string message, Exception innerException)
        : base(message, innerException)
    {
        Operation = string.Empty;
    }

    /// <summary>
    /// Gets the operation that was being performed when the exception occurred.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Gets the key that was being operated on (e.g., short URL).
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with operation details.
    /// </summary>
    /// <param name="operation">The operation that was being performed.</param>
    /// <param name="key">The key that was being operated on.</param>
    /// <param name="message">The error message.</param>
    public RepositoryException(string operation, string? key, string message)
        : base($"Repository operation '{operation}' failed for key '{key ?? "unknown"}': {message}")
    {
        Operation = operation ?? string.Empty;
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with operation details and inner exception.
    /// </summary>
    /// <param name="operation">The operation that was being performed.</param>
    /// <param name="key">The key that was being operated on.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RepositoryException(string operation, string? key, string message, Exception innerException)
        : base($"Repository operation '{operation}' failed for key '{key ?? "unknown"}': {message}", innerException)
    {
        Operation = operation ?? string.Empty;
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
    protected RepositoryException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Operation = info.GetString(nameof(Operation)) ?? string.Empty;
        Key = info.GetString(nameof(Key));
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

        info.AddValue(nameof(Operation), Operation);
        info.AddValue(nameof(Key), Key);

        base.GetObjectData(info, context);
    }
}
