# URL Bench

**URL Bench** is a project for testing how different URL shorteners and storage methods work in .NET.  

It helps answer two questions:

- How fast can we shorten and store URLs?
- Which method works best when many requests happen at the same time?

---

## Quick Start (5 minutes)

### 1. Install prerequisites

- Windows, macOS, or Linux
- Git: [Download Git](https://git-scm.com/downloads)
- .NET 9 SDK: [Download .NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

Check your install:

```sh
dotnet --info
git --version
```

The repo uses .NET SDK 9.0.304 (or later).

### 2. Build and run

```sh
# Clone this repo
git clone https://github.com/<your-org>/url-bench
cd url-bench

# Build the app (release mode)
dotnet build -c Release

# Run a simple benchmark
dotnet run --project ./src/Console --framework net9.0 -- bench -n 10000 -i 1 -t IncrementingIntegerUrlShortener -s ConcurrentUrlRepository
```

This will:

- Shorten and store 10,000 URLs one time
- Show how many operations per second your computer can handle

## Core Ideas

URL Bench has two main parts: shorteners and repositories.

- **Shortener**: makes a short code from a long URL. Example: `https://example.com/very/long/url → abc123`
- **Repository**: saves and looks up the mapping. Example: `(abc123 → https://example.com/very/long/url)`

## The Contracts

All shorteners and repositories follow these interfaces:

```csharp
public interface IUrlRepository
{
  string GetOriginalUrl(string shortUrl); 
  bool SaveUrlMapping(string shortUrl, string originalUrl); 
  bool TrySaveUrlMapping(string shortUrl, string originalUrl, string? expectedValue, out string? currentValue); 
}

public interface IUrlShortener
{
  string ShortenUrl(string? originalUrl); 
}
```

> Notes:
>
> - `ShortenUrl`; if `originalUrl` is null/empty, return "" (don't throw).
> - `TrySaveUrlMapping` → safe save that prevents lost updates. See [Advanced: CAS][#advanced-compare-and-set-cas] below.

## Available Implementations

### Shorteners

- IncrementingIntegerUrlShortener – fastest; unique only during one run
- SnowflakeUrlShortener – unique IDs across machines; time-ordered
- VectorizedUrlShortener – fastest hashing on supported CPUs (SIMD)
- Murmur3HashUrlShortener – non-cryptographic hash; deterministic
- Sha256HashUrlShortener – cryptographic hash; slow but secure

### Repositories

- ConcurrentUrlRepository – simple, thread-safe baseline
- ReaderWriterLockSlimUrlRepository – single dictionary with lock
- PartitionedUrlRepository – multiple dictionaries with shard locks
- MemoryCacheUrlRepository – ASP.NET Core memory cache with expiry
- FastCacheUrlRepository – uses fast-cache (fast lookups + expiry)
- FASTERUrlRepository – optimized key-value store, high throughput

## Development Setup

From the repo root

```sh
# Check formatting
dotnet format --verify-no-changes

# Strict build
dotnet build -c Release /p:PedanticMode=true

# Run tests
dotnet test --configuration Release --settings ./build/targets/tests/test.runsettings
```

## Running the Console

The console app lets you run your own benchmarks

Options:

```sh
-t <SHORTENER TYPE>
-s <STORAGE TYPE>
-n <NUMBER OF URLS>
-i <ITERATION COUNT>
-u <URL TO USE>
-p <PARALLELISM DEGREE>
```

Example:

```sh
dotnet run --project ./src/Console -c Release --framework net9.0 -- bench -t VectorizedUrlShortener -s ConcurrentUrlRepository -n 500000 -i 2
```

If no options are given, defaults are:

```sh
-t IncrementingIntegerUrlShortener
-s FASTERUrlRepository
-i 3
-n 10000000
-u https://www.microsoft.com
-p 1
```

### Console Output

The console shows:

- Iterations and number of URLs processed
- Inserts or reads per second
- Memory usage
- Stats like averages, min/max, and outliers

This helps compare different shorteners and repositories

## Advanced: Compare-and-Set (CAS)

When many programs or threads write the same key, a normal save may overwrite someone else’s value.
CAS = “only save if the current value is what I expect.”

How it works:

- Success → store updates safely
- Failure → store unchanged; you get the actual current value

Patterns:

- Insert if absent: expected = null
- Update if unchanged: expected = old value
- Retry loop: if fail, read current value and try again

This allows safe updates without heavy locks.

## Benchmarks

We also include a BenchmarkDotNet project for deeper performance testing.

Run with:

```sh
dotnet run -c Release --framework net9.0 --project ./src/Benchmark/Benchmark.csproj
```

> Note: repository benchmarks may take ~20 minutes, shortener benchmarks ~5 minutes.

## Tests

Unit tests use xUnit.
Each implementation of IUrlShortener and IUrlRepository is tested automatically.

Run tests:

```sh
dotnet test -c Release --settings ./build/targets/tests/test.runsettings
```

> This will build the project and run all tests in the release configuration.

## Troubleshooting the Console

- Unrecognized type 'X' → Run without args to see defaults or check class names under src/Logic/Shorteners and src/Logic/Repositories.
- Very slow / high memory → Try -n 100000 -i 1.
- Vectorized shortener fails → Your CPU may not support AVX2/SSE2/AdvSimd. Use Murmur3 or Sha256.

## Glossary

- **Throughput** – operations per second (higher = faster).
- **SIMD** – CPU feature for processing many values at once.
- **BenchmarkDotNet** – library to run accurate performance benchmarks.
- **Repository** – storage for URL mappings.
- **Shortener** – generates short codes for URLs.
- **CAS (Compare-and-Set)** – safe update pattern to prevent lost writes.
