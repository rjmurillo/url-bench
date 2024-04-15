using Logic.Repositories;

internal static class UrlRepositoryHelper
{
    public static IEnumerable<Type> GetConcreteTypes()
    {
        var a = typeof(IUrlRepository).Assembly;
        return a
            .GetTypes()
            .Where(t => typeof(IUrlRepository).IsAssignableFrom(t) &&
                        t is { IsInterface: false, IsAbstract: false });

    }

    public static Type GetType(string? typeName)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(typeName);

        var typesImplementingIUrlRepository = GetConcreteTypes();

        return typesImplementingIUrlRepository.FirstOrDefault(t =>
            t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }
}