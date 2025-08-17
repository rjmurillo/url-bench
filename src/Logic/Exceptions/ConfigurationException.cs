using System.Runtime.Serialization;

namespace Logic.Exceptions;

/// <summary>
/// Exception thrown when configuration is invalid or missing.
/// This exception indicates that a configuration parameter is invalid,
/// missing, or contains an unsupported value.
/// </summary>
[Serializable]
public class ConfigurationException : Exception
{
    /// <summary>
    /// Gets the name of the configuration parameter that caused the exception.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets the value of the configuration parameter that caused the exception.
    /// </summary>
    public string? ParameterValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
    {
        ParameterName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message) : base(message)
    {
        ParameterName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
        ParameterName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with parameter details.
    /// </summary>
    /// <param name="parameterName">The name of the configuration parameter.</param>
    /// <param name="parameterValue">The value of the configuration parameter.</param>
    /// <param name="message">The error message.</param>
    public ConfigurationException(string parameterName, string? parameterValue, string message)
        : base($"Configuration error for parameter '{parameterName}' with value '{parameterValue ?? "null"}': {message}")
    {
        ParameterName = parameterName ?? string.Empty;
        ParameterValue = parameterValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with parameter details and inner exception.
    /// </summary>
    /// <param name="parameterName">The name of the configuration parameter.</param>
    /// <param name="parameterValue">The value of the configuration parameter.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string parameterName, string? parameterValue, string message, Exception innerException)
        : base($"Configuration error for parameter '{parameterName}' with value '{parameterValue ?? "null"}': {message}", innerException)
    {
        ParameterName = parameterName ?? string.Empty;
        ParameterValue = parameterValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ParameterName = info.GetString(nameof(ParameterName)) ?? string.Empty;
        ParameterValue = info.GetString(nameof(ParameterValue));
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

        info.AddValue(nameof(ParameterName), ParameterName);
        info.AddValue(nameof(ParameterValue), ParameterValue);

        base.GetObjectData(info, context);
    }
}
