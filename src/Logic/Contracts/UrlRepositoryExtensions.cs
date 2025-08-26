using System.Runtime.CompilerServices;

namespace Logic.Contracts;

/// <summary>
/// Extensions for <see cref="IUrlRepository"/> enabling span-based lookup without changing interfaces.
/// </summary>
public static class UrlRepositoryExtensions
{
    /// <summary>
    /// Performs a lookup using a read-only character span. Zero allocation on
    /// input when the receiver implements <see cref="IUrlRepositorySpan"/>.
    /// Otherwise, fall back to the legacy string path (one allocation). Output
    /// is materialized as a string to satisfy the interface contract.
    /// </summary>
    /// <remarks>
    /// This method mirrors the null/empty semantics of the string-based <see cref="IUrlRepository"/>:
    /// passing an empty span is equivalent to passing <see cref="string.Empty" />.
    /// </remarks>
    /// <param name="repository">The repository instance. Must not be <see langword="null"/>.</param>
    /// <param name="shortUrl">The short URL to look up as a span. Empty is allowed and equivalent to <see cref="string.Empty" />.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="repository"/> is <see langword="null" />.</exception>
    /// <returns>The original URL if found; otherwise, an <see cref="string.Empty"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOriginalUrl(this IUrlRepository repository, scoped ReadOnlySpan<char> shortUrl)
    {
        ArgumentNullException.ThrowIfNull(repository);

        // We cannot rely on JIT devirtualization. Even if the interface call devirtualizes, the bound
        // signature remains string overload of IUrlRepository. The JIT will not switch to another
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
        if (repository is IUrlRepositorySpan spanCapable)
        {
            return spanCapable.GetOriginalUrl(shortUrl);
        }

        // Legacy path: must materialize the input for the string-taking API.
        return GetOriginalUrlSlow(repository, shortUrl);
    }

    /// <summary>
    /// Span-based slow path used when the repository isn't span-capable. We keep this path
    /// predictable and fast for common cases: allocating the span to a string when needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetOriginalUrlSlow(IUrlRepository repository, ReadOnlySpan<char> shortUrl)
    {
        // Allocate exactly once to satisfy the legacy string-based API.
        return repository.GetOriginalUrl(new string(shortUrl));
    }
}
