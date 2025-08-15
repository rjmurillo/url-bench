using System.Diagnostics;
using Contracts;
using Logic;

namespace UrlConverterPermutations;

public class ShortenerTheoryData : TheoryData<IUrlShortener>
{
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