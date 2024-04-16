using Contracts;

namespace Logic;

public static class UrlRepositoryHelper
{
    public static IEnumerable<Type> GetConcreteTypes<T>()
    {
        var a = typeof(T).Assembly;
        return a
            .GetTypes()
            .Where(t => typeof(T).IsAssignableFrom(t) &&
                        t is { IsInterface: false, IsAbstract: false });

    }

    public static Type? GetRepositoryType(string? typeName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
#endif

        var typesImplementingIUrlRepository = GetConcreteTypes<IUrlRepository>();

        return typesImplementingIUrlRepository.FirstOrDefault(t =>
            t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }

    public static Type? GetShortenerType(string? typeName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
#else
        //if (string.IsNullEmptyOrWhiteSpace(typeName))
        //{
        //    throw new ArgumentNullException(nameof(typeName));
        //}
#endif

        var typesImplementingIUrlShortener = GetConcreteTypes<IUrlShortener>();

        return typesImplementingIUrlShortener.FirstOrDefault(t =>
                       t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }
}