using Logic.Contracts;

using System.Runtime.CompilerServices;

namespace Logic;

/// <summary>
/// Extensions for <see cref="IUrlManager"/> enabling span-based lookup without changing interfaces.
/// </summary>
public static class UrlManagerExtensions
{
    /// <summary>
    /// Performs a lookup using a read-only character span. Zero allocation on
    /// input when the receiver implements IUrlManagerSpan. Otherwise falls back
    /// to the legacy string path (one allocation). Output is materialized as a
    /// string to satisfy the interface contract.
    /// </summary>
    /// <param name="manager">The manager instance.</param>
    /// <param name="shortUrl">The short URL to look up as a span.</param>
    /// <returns>The original URL if found; otherwise, an empty string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOriginalUrl(this IUrlManager manager, scoped ReadOnlySpan<char> shortUrl)
    {
        // We cannot rely on JIT devirtualization. Even if the interface call devirtualizes, the bound
        // signature remains string overload of IUrlManager. The JIT will not switch to another
        // overload if we're bound by the interface contract. We also can't do clever things with
        // dynamic: ReadOnlySpan<T> is a ref struct and cannot be boxed, so dynamic cannot dispatch to
        // a span overload. The last clever thing we can do is with unsafe char tricks and pointers.
        // The ergonomics of that are 1) gross, and 2) introduces a large foot gun. And we still need
        // to materialize a managed string to satisfy the interface, so we gain little by adding the
        // complication.
        //
        // This interface advertises the span capability. The extension method performs a single type test
        // and routes to the fast path when present. This is optimizing for a few things:
        // 1. No changes to the legacy interface. Adding support for Span<T> is additive
        // 2. Callers can pass spans through the interface variable. We're using inheritance of the interface
        //    to maintain the contract.
        // 3. The branch is predictable and can be optimized in hot paths.
        // 4. Avoiding allocations with Span<T> is opt in.
        if (manager is IUrlManagerSpan spanCapable)
        {
            ReadOnlySpan<char> span = spanCapable.GetOriginalUrl(shortUrl);

            // Materialize once for the string-returning interface.
            return new string(span);
        }

        // Legacy path: must materialize the input for the string-taking API.
        return GetOriginalUrlSlow(manager, shortUrl);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetOriginalUrlSlow(IUrlManager manager, ReadOnlySpan<char> shortUrl)
    {
        // Fallback to legacy string-based lookup
        return manager.GetOriginalUrl(new string(shortUrl));
    }
}