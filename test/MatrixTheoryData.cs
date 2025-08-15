using Contracts;
using Logic;

namespace UrlConverterPermutations;

public class MatrixTheoryData : TheoryData<IUrlShortener, IUrlRepository>
{
    public MatrixTheoryData()
    {
        var shorteners = UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>().ToArray();
        var repositories = UrlRepositoryHelper.GetConcreteTypes<IUrlRepository>().ToArray();

        foreach (var shortener in shorteners)
        {
            foreach (var repository in repositories)
            {
                var (s, r) = Factory.TypeFactory(repository, shortener);
                Add(s, r);
            }
        }
    }
}