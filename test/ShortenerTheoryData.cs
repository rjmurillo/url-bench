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
            var s = Factory.CreateShortener(shortener);
            Debug.Assert(s != null, nameof(s) + " != null");
            Add(s);
        }
    }
}