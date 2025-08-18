using Contracts;
using Logic;

namespace UrlConverterPermutations;

/// <summary>
/// Provides theory data for testing URL shortener implementations.
/// </summary>
public class ShortenerTheoryData : TheoryData<IUrlShortener>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShortenerTheoryData"/> class and populates it with all available URL shortener implementations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a shortener cannot be created due to missing parameterless constructor.</exception>
    public ShortenerTheoryData()
    {
        var shorteners = UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>()
                                        .OrderBy(t => t.Name, StringComparer.Ordinal)
                                        .ToArray();

        foreach (var shortener in shorteners)
        {
            var s = Factory.CreateShortener(shortener)
                                ?? throw new InvalidOperationException($"Failed to create {shortener.FullName}. Ensure a public parameterless constructor.");
            Add(s);
        }
    }
}
