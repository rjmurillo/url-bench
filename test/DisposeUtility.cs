namespace UrlConverterPermutations;

/// <summary>
/// Provides utility methods for disposing objects safely.
/// </summary>
internal static class DisposeUtility
{
    /// <summary>
    /// Attempts to dispose the specified object if it implements IDisposable.
    /// </summary>
    /// <param name="m">The object to dispose.</param>
    public static void TryDispose(this object m)
    {
        if (m is IDisposable disposable)
        {
            ((IDisposable)disposable).Dispose();
        }
    }
}
