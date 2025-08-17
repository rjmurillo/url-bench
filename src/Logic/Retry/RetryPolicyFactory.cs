using Logic.Exceptions;
using Polly;
using Polly.Retry;

namespace Logic.Retry;

/// <summary>
/// Factory for creating Polly v8 retry policies with default configurations.
/// </summary>
public static class RetryPolicyFactory
{
    /// <summary>
    /// Creates a synchronous retry policy with exponential backoff and jitter using Polly v8.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts. Must be positive.</param>
    /// <param name="baseDelayMs">Base delay in milliseconds for exponential backoff. Must be positive.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds to cap exponential growth. Must be greater than baseDelayMs.</param>
    /// <returns>A configured retry policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public static ResiliencePipeline CreateRetryPolicy(int maxRetries = 3, int baseDelayMs = 10, int maxDelayMs = 1000)
    {
        var baseDelay = TimeSpan.FromMilliseconds(baseDelayMs);
        var maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);

        if (maxRetries <= 0)
        {
            throw new ConfigurationException(nameof(maxRetries), maxRetries.ToString(), "Max retries must be positive.");
        }

        if (baseDelay <= TimeSpan.Zero)
        {
            throw new ConfigurationException(nameof(baseDelayMs), baseDelayMs.ToString(), "Base delay must be positive.");
        }

        if (maxDelay < baseDelay)
        {
            throw new ConfigurationException(nameof(maxDelayMs), maxDelayMs.ToString(), "Max delay must be greater than base delay.");
        }

        var retry = new RetryStrategyOptions
        {
            MaxRetryAttempts = maxRetries,
            Delay = baseDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<Exception>(IsRetryable),
            MaxDelay = maxDelay,
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retry)
            .Build();
    }

    /// <summary>
    /// Creates an asynchronous retry policy with exponential backoff and jitter using Polly v8.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts. Must be positive.</param>
    /// <param name="baseDelayMs">Base delay in milliseconds for exponential backoff. Must be positive.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds to cap exponential growth. Must be greater than baseDelayMs.</param>
    /// <returns>A configured async retry policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public static ResiliencePipeline CreateAsyncRetryPolicy(int maxRetries = 3, int baseDelayMs = 10, int maxDelayMs = 1000)
    {
        // In Polly v8, the same ResiliencePipeline handles both sync and async operations
        return CreateRetryPolicy(maxRetries, baseDelayMs, maxDelayMs);
    }

    /// <summary>
    /// Determines if an exception is retryable.
    /// </summary>
    /// <param name="ex">The exception to evaluate.</param>
    /// <returns>True if the exception should trigger a retry; otherwise, false.</returns>
    private static bool IsRetryable(Exception ex) =>
        ex is not OperationCanceledException
        && ex is not OutOfMemoryException
        && ex is not StackOverflowException
        && ex is not AccessViolationException
        && ex is not ThreadAbortException;
}
