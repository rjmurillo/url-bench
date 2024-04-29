# URL Bench

**URL Bench** is a set of benchmarks built around two interfaces:

```csharp
public interface IUrlShortener
{
    string ShortenUrl(string originalUrl);
}

public interface IUrlRepository
{
    string GetOriginalUrl(string shortUrl);
    bool SaveUrlMapping(string shortUrl, string originalUrl);
}
```

There are implementations of each utilizing various .NET features and capabilities, as well as popular libraries. 

Shorteners include:

- `IncrementintInteger` making use of `Interlocked.Increment`
- `Murmur3Hash` making use of the non-cryptographic [MurmurHash](https://en.wikipedia.org/wiki/MurmurHash)
- `Sha256` making use of the cryptographic SHA-256 hash.
- `Snowflake` making use of [Snowflake ID](https://en.wikipedia.org/wiki/Snowflake_ID)
- `Vectorized` non-cryptographic hashing making use of hardware accelerated AVX, SSE2, or ARM SIMD

Repositories include:

- `Concurrent` making use of the built in .NET  `ConncurrentDictionary<string, string>` type
- `FastCache` making use of [fast-cache](https://github.com/neon-sunset/fast-cache) to include items with expiration time
- `FASTER` using a [concurrent key-value store + cache](https://aka.ms/faster)
- `IMemoryCache` using [ASP.NET Core's built in memory-based cache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-8.0)
- `Partitioned` using a naive array of `Dictionary<string,string>` and `ReaderWriterLockSlim`.
- `ReaderWriterLockSlim` using a single `Diction<string,string>` and `ReaderWriterLockSlim`

The repository also contains utility methods for performing conversions, building strings without heap allocations, and wiring together the shortener and repository through a `Facade`. Unit tests are provided for the shorteners and repositories through xUnit's `TheoryData<,>` construct: each implementation of `IUrlShortener` and `IUrlRepository` is automatically detected and placed into respective theory-based tests.

Various benchmarks are also provided to test capabilities of different types of shorteners and repositories in throughput and multi-threaded scenarios.