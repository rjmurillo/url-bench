using System.Diagnostics;
using Contracts;
using Logic.Contracts;

namespace Logic;

/// <summary>
/// Provides factory methods for creating URL shorteners, repositories, and collision resolution strategies.
/// </summary>
public static class Factory
{
    /// <summary>
    /// Creates a URL shortener and repository pair from the specified types.
    /// </summary>
    /// <param name="repositoryType">The type of repository to create.</param>
    /// <param name="shortenerType">The type of shortener to create.</param>
    /// <returns>A tuple containing the created shortener and repository.</returns>
    /// <exception cref="DebugAssertException">Thrown when the shortener or repository cannot be created.</exception>
    public static (IUrlShortener, IUrlRepository) TypeFactory(Type repositoryType, Type shortenerType)
    {
        var repository = Activator.CreateInstance(repositoryType) as IUrlRepository;
        var shortener = Activator.CreateInstance(shortenerType) as IUrlShortener;

        Debug.Assert(shortener != null, nameof(shortener) + " != null");
        Debug.Assert(repository != null, nameof(repository) + " != null");

        return (shortener, repository);
    }

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

    /// <summary>
    /// Creates a collision resolution strategy instance from the specified type.
    /// </summary>
    /// <param name="strategyType">The type of collision resolution strategy to create.</param>
    /// <returns>The created collision resolution strategy instance, or null if creation fails.</returns>
    public static ICollisionResolutionStrategy? CreateCollisionResolutionStrategy(Type strategyType)
    {
        return Activator.CreateInstance(strategyType) as ICollisionResolutionStrategy;
    }
}
