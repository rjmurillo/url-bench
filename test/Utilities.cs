using Contracts;
using Logic.Shorteners;

namespace UrlConverterPermutations;

internal static class Utilities
{
    internal static bool IsDeterministicHashShortener(IUrlShortener? shortener)
    {
        return shortener is VectorizedUrlShortener or
               Murmur3HashUrlShortener or
               Sha256HashUrlShortener;
    }
}
