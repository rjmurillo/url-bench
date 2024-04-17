using Contracts;


namespace UrlConverterPermutations;

public static class Factory
{
    public static (IUrlShortener, IUrlRepository) TypeFactory(Type repositoryType, Type shortenerType)
    {
        var repository = Activator.CreateInstance(repositoryType) as IUrlRepository;
        var shortener = Activator.CreateInstance(shortenerType) as IUrlShortener;

        return (shortener, repository);
    }

    public static IUrlShortener CreateShortener(Type shortenerType)
    {
        return Activator.CreateInstance(shortenerType) as IUrlShortener;
    }

    public static IUrlRepository CreateRepository(Type repositoryType)
    {
        return Activator.CreateInstance(repositoryType) as IUrlRepository;
    }

    public static T TypeFactory<T>()
    {
        return (T)Activator.CreateInstance(typeof(T));
    }
}