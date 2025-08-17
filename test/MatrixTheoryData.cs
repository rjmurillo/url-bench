using Contracts;
using Logic;

namespace UrlConverterPermutations;

/// <summary>
/// Provides theory data for testing combinations of URL shorteners and repositories.
/// Generates fresh instances for each enumeration to avoid sharing disposed instances across tests.
/// </summary>
public class MatrixTheoryData : IEnumerable<object[]>
{    
    public IEnumerator<object[]> GetEnumerator()
    {
        var shortenerTypes = UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>().OrderBy(t => t.FullName).ToArray();
        var repositoryTypes = UrlRepositoryHelper.GetConcreteTypes<IUrlRepository>().OrderBy(t => t.FullName).ToArray();

        foreach (var shortenerType in shortenerTypes)
        {
            foreach (var repositoryType in repositoryTypes)
            {
                var (s, r) = Factory.TypeFactory(repositoryType, shortenerType);
                yield return new object[] { s, r };
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}