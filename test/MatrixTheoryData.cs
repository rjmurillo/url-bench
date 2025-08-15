using Contracts;
using Logic;

namespace UrlConverterPermutations;

/// <summary>
/// Provides theory data for testing combinations of URL shorteners and repositories.
/// </summary>
public class MatrixTheoryData : TheoryData<IUrlShortener, IUrlRepository>
{
    /// <summary>
    /// Initializes a new instance of the MatrixTheoryData class and populates it with all combinations of URL shorteners and repositories.
    /// </summary>
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