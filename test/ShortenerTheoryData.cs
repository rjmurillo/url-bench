using Contracts;
using Logic;

namespace UrlConverterPermutations;

public class ShortenerTheoryData : TheoryData<IUrlShortener>
{
    public ShortenerTheoryData()
    {
        var shorteners = UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>().ToArray();


        foreach (var shortener in shorteners)
        {
            var s = Factory.CreateShortener(shortener);
            Add(s);
        }
    }
}