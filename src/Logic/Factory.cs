using Contracts;

namespace Logic;

/// <summary>
/// Factory class for creating URL shorteners and repositories.
/// </summary>
public static class Factory
{
    /// <summary>
    /// Creates a URL shortener instance from the specified type.
    /// </summary>
    /// <param name="shortenerType">The type of shortener to create.</param>
    /// <returns>The created shortener instance, or null if creation fails.</returns>
    public static IUrlShortener? CreateShortener(Type shortenerType)
    {
        return Activator.CreateInstance(shortenerType) as IUrlShortener;
    }

    /// <summary>
    /// Creates a URL repository instance from the specified type.
    /// </summary>
    /// <param name="repositoryType">The type of repository to create.</param>
    /// <returns>The created repository instance, or null if creation fails.</returns>
    public static IUrlRepository? CreateRepository(Type repositoryType)
    {
        return Activator.CreateInstance(repositoryType) as IUrlRepository;
    }
}
