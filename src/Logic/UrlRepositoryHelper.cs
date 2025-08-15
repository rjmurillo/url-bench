using Contracts;

namespace Logic;

/// <summary>
/// Provides helper methods for working with URL repository and shortener types.
/// </summary>
public static class UrlRepositoryHelper
{
    /// <summary>
    /// Gets all concrete types that implement the specified interface type.
    /// </summary>
    /// <typeparam name="T">The interface type to find implementations for.</typeparam>
    /// <returns>An enumerable of concrete types that implement the specified interface.</returns>
    public static IEnumerable<Type> GetConcreteTypes<T>()
    {
        var a = typeof(T).Assembly;
        return a
            .GetTypes()
            .Where(t => typeof(T).IsAssignableFrom(t) &&
                        t is { IsInterface: false, IsAbstract: false });

    }

    /// <summary>
    /// Gets the repository type by name.
    /// </summary>
    /// <param name="typeName">The name of the repository type to find.</param>
    /// <returns>The repository type if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when typeName is null, empty, or whitespace.</exception>
    public static Type? GetRepositoryType(string? typeName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
#endif

        var typesImplementingIUrlRepository = GetConcreteTypes<IUrlRepository>();

        return typesImplementingIUrlRepository.FirstOrDefault(t =>
            t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the shortener type by name.
    /// </summary>
    /// <param name="typeName">The name of the shortener type to find.</param>
    /// <returns>The shortener type if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when typeName is null, empty, or whitespace.</exception>
    public static Type? GetShortenerType(string? typeName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
#else
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }
#endif

        var typesImplementingIUrlShortener = GetConcreteTypes<IUrlShortener>();

        return typesImplementingIUrlShortener.FirstOrDefault(t =>
                       t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }
}