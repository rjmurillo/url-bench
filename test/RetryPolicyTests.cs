using Logic.Retry;
using Polly;

namespace UrlConverterPermutations;

public class RetryPolicyTests
{
    [Fact]
    public void CreateRetryPolicy_WithSuccessfulOperation_ReturnsResultImmediately()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(3, 10);
        var operation = new Func<string>(() => "success");

        // Act
        var result = policy.Execute(operation);

        // Assert
        Assert.Equal("success", result);
    }

    [Fact]
    public void CreateRetryPolicy_WithFailingOperation_RetriesAndEventuallySucceeds()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(3, 1); // Short delay for testing
        var attemptCount = 0;
        var operation = new Func<string>(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Temporary failure");
            }
            return "success";
        });

        // Act
        var result = policy.Execute(operation);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public void CreateRetryPolicy_WithAlwaysFailingOperation_ThrowsAfterMaxRetries()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(2, 1);
        var operation = new Func<string>(() => throw new InvalidOperationException("Always fails"));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => policy.Execute(operation));
        Assert.Equal("Always fails", exception.Message);
    }

    [Fact]
    public void CreateRetryPolicy_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(3, 10);
        var cts = new CancellationTokenSource();
        var operation = new Func<string>(() =>
        {
            cts.Token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Failure");
        });

        // Act
        cts.Cancel();

        // Assert
        Assert.Throws<OperationCanceledException>(() => policy.Execute(operation));
    }

    [Fact]
    public void CreateRetryPolicy_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(3, 10);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.Execute<string>(null!));
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithSuccessfulOperation_ReturnsResultImmediately()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(3, 10);

        // Act
        var result = await policy.ExecuteAsync(async (CancellationToken token) => "success");

        // Assert
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithFailingOperation_RetriesAndEventuallySucceeds()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(3, 1); // Short delay for testing
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async (CancellationToken token) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Temporary failure");
            }
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithAlwaysFailingOperation_ThrowsAfterMaxRetries()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(2, 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await policy.ExecuteAsync(async (CancellationToken token) => throw new InvalidOperationException("Always fails")));
        Assert.Equal("Always fails", exception.Message);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(3, 10);
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => 
            await policy.ExecuteAsync(async (CancellationToken token) =>
            {
                cts.Token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Failure");
            }));
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(3, 10);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await policy.ExecuteAsync<string>(null!));
    }

    [Fact]
    public void CreateRetryPolicy_WithInvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Test invalid maxRetries
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateRetryPolicy(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateRetryPolicy(-1, 10));

        // Test invalid baseDelayMs
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateRetryPolicy(3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateRetryPolicy(3, -1));

        // Test invalid maxDelayMs
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateRetryPolicy(3, 10, 5)); // maxDelay < baseDelay
    }

    [Fact]
    public void CreateAsyncRetryPolicy_WithInvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Test invalid maxRetries
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateAsyncRetryPolicy(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateAsyncRetryPolicy(-1, 10));

        // Test invalid baseDelayMs
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateAsyncRetryPolicy(3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateAsyncRetryPolicy(3, -1));

        // Test invalid maxDelayMs
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicyFactory.CreateAsyncRetryPolicy(3, 10, 5)); // maxDelay < baseDelay
    }

    [Fact]
    public void CreateRetryPolicy_WithNonRetryableException_ThrowsImmediately()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(3, 10);

        // Act & Assert
        var exception = Assert.Throws<OutOfMemoryException>(() => 
            policy.Execute(() => throw new OutOfMemoryException("Non-retryable")));
        Assert.Equal("Non-retryable", exception.Message);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithNonRetryableException_ThrowsImmediately()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(3, 10);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OutOfMemoryException>(async () => 
            await policy.ExecuteAsync(async (CancellationToken token) => throw new OutOfMemoryException("Non-retryable")));
        Assert.Equal("Non-retryable", exception.Message);
    }

    [Fact]
    public void CreateRetryPolicy_WithDifferentReturnTypes_WorksCorrectly()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateRetryPolicy(3, 1);

        // Act
        var intResult = policy.Execute(() => 42);
        var boolResult = policy.Execute(() => true);

        // Assert
        Assert.Equal(42, intResult);
        Assert.True(boolResult);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_WithDifferentReturnTypes_WorksCorrectly()
    {
        // Arrange
        var policy = RetryPolicyFactory.CreateAsyncRetryPolicy(3, 1);

        // Act
        var intResult = await policy.ExecuteAsync(async (CancellationToken token) => 42);
        var boolResult = await policy.ExecuteAsync(async (CancellationToken token) => true);

        // Assert
        Assert.Equal(42, intResult);
        Assert.True(boolResult);
    }
}
