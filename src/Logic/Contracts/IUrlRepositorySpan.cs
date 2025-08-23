using Contracts;

namespace Logic.Contracts;

/// <summary>
/// Defines a contract for a URL repository that supports retrieval of the original URL using a <see cref="ReadOnlySpan{Char}"/>.
/// </summary>
public interface IUrlRepositorySpan : IUrlRepository
{
    /// <summary>
    /// Retrieves the original URL associated with the specified short URL as a <see cref="ReadOnlySpan{Char}"/>.
    /// </summary>
    /// <param name="shortUrl">The short URL to look up as a <see cref="ReadOnlySpan{T}" />.</param>
    /// <returns>The original URL as a <see cref="ReadOnlyMemory{Char}"/> if found; otherwise, <see cref="ReadOnlyMemory{T}.Empty"/>.</returns>
    /// <remarks>The return as <see cref="ReadOnlyMemory{T}"/> is required to avoid reallocating a <see cref="string"/> in the compatibility layers above.</remarks>
    string GetOriginalUrl(scoped ReadOnlySpan<char> shortUrl);
}
