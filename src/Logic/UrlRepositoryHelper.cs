using Contracts;
using Logic.Repositories;
using Logic.Shorteners;

namespace Logic;

/// <summary>
/// Helper class for working with URL repositories and shorteners.
/// </summary>
public static class UrlRepositoryHelper
{
    /// <summary>
    /// Gets all concrete types that implement the specified interface.
    /// </summary>
    /// <typeparam name="T">The interface type to find implementations for.</typeparam>
    /// <returns>An array of concrete types that implement the specified interface.</returns>
    public static Type[] GetConcreteTypes<T>()
    {
        var interfaceType = typeof(T);
        var types = interfaceType.Assembly.GetTypes();

        return types.Where(t => !t.IsAbstract && !t.IsInterface && interfaceType.IsAssignableFrom(t)).ToArray();
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
#else
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }
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
