using Contracts;
using Logic.Shorteners;

namespace UrlConverterPermutations;

internal static class Utilities
{
    internal static bool IsDeterministicHashShortener(IUrlShortener shortener)
    {
        var shortenerType = shortener.GetType();
        return shortenerType == typeof(VectorizedUrlShortener) ||
               shortenerType == typeof(Murmur3HashUrlShortener) ||
               shortenerType == typeof(Sha256HashUrlShortener);
    }
}