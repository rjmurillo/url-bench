using Contracts;
using Logic.Contracts;

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
        Type interfaceType = typeof(T);
        Type[] types = interfaceType.Assembly.GetTypes();
        var result = new List<Type>();

        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            if (!type.IsAbstract && !type.IsInterface && interfaceType.IsAssignableFrom(type))
            {
                result.Add(type);
            }
        }

        return result.ToArray();
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

        Type[] typesImplementingIUrlRepository = GetConcreteTypes<IUrlRepository>();

        for (int i = 0; i < typesImplementingIUrlRepository.Length; i++)
        {
            Type type = typesImplementingIUrlRepository[i];
            if (type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return null;
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

        Type[] typesImplementingIUrlShortener = GetConcreteTypes<IUrlShortener>();

        for (int i = 0; i < typesImplementingIUrlShortener.Length; i++)
        {
            Type type = typesImplementingIUrlShortener[i];
            if (type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return null;
    }
}
