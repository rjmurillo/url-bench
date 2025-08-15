namespace UrlConverterPermutations;

internal static class DisposeUtility
{
    public static void TryDispose(this object m)
    {
        if (m is IDisposable disposable)
        {
            ((IDisposable)disposable).Dispose();
        }
    }
}