# URL Bench

**Design a URL shortener.** This is an open-ended system design question used by Richard Murillo during technical interviews. This repository specifically identifies the following features:

1. This is the implementation that may be exposed by an API or full featured web app. The scope is limited and we have benchmarks.
2. Since it's only the core logic, we don't have an API, nor authentication, or logging.
3. We are not permitting modification or deleting links. The intent is to have the focus be on the shortening and storage algorithm to fit within a 30 minute technical interview session.
4. Since we cannot delete links, they persist forever.
5. We do not permit choosing your shortlink, the system always auto-generates it.

## Design goals

1. We should be able to store _a lot_ of links, since we are not automatically expiring them. 10 years.
2. Short links should be _as short as possible_. The whole point of a link shortener is to make _short_ links.
3. Following a short link should be _fast_.

> **How many distinct short URLs do we need?** On a busy day, we create 100,000 per minute or `100,000 * 60 * 24 ≈ 52.5` billion a year. If we keep things for "almost forever", we can multiply by 100 years. So that's 5.2 _trillion_ short URLs.

## This repository

The goal of this repository specifically is to provide some common example implementations and measure how fast different URL shorteners and storage methods work in [.NET](https://learn.microsoft.com/dotnet/fundamentals/) using simple, repeatable tests.

It helps answer:

- How fast can we shorten and store URLs? See [throughput](https://en.wikipedia.org/wiki/Throughput).
- Which method works best when many requests happen at the same time? See [concurrency](https://martinfowler.com/articles/patterns-of-concurrent-architecture.html).

> If you're new to [.NET and C#](https://learn.microsoft.com/dotnet/csharp/) check out [Learn C#](https://dotnet.microsoft.com/en-us/learn/csharp).

---

## Quick Start (5 minutes)

### 1) Install prerequisites

- Windows, macOS, or Linux
- Git: [Download Git](https://git-scm.com/downloads)
- .NET 9 SDK: [Download .NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

Check your install:

```powershell
dotnet --info
git --version
```

This repo pins SDK 9.0.304 (or later) in `global.json`.

### 2) Build and run a small test

```powershell
# Clone this repo
git clone https://github.com/rjmurillo/url-bench
cd url-bench

# Restore tools
dotnet tool restore

# Build (Release)
dotnet build -c Release

# Run a short benchmark using the incrementing integer shortener and a concurrent dictionary repository
dotnet run --project ./src/Console --framework net9.0 -- bench -t IncrementingIntegerUrlShortener -s ConcurrentUrlRepository
```

What you'll see:

- It shortens and stores 10,000,000 URLs three times.
- It prints operations per second, memory, and basic statistics.

## Core ideas

URL Bench has two building blocks:

> If you're new to [.NET and C#](https://learn.microsoft.com/dotnet/csharp/) check out [Learn C#](https://dotnet.microsoft.com/en-us/learn/csharp).

- Shortener: makes a short code from a long URL. Example: `https://example.com/very/long/url -> abc123`.  Learn about [URL shorteners](https://en.wikipedia.org/wiki/URL_shortening).
- Repository: stores and looks up the mapping `(short → original)`. This is commonly implemented as some sort of HashMap, Dictionary, or Key Value pair implementation. Learn about [key-value stores](https://en.wikipedia.org/wiki/Key%E2%80%93value_database).

## Contracts (interfaces)

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
> - `ShortenUrl`: if `originalUrl` is null or empty, return an empty string (don't throw).
> - `TrySaveUrlMapping`: safe save to avoid lost updates. See [Advanced: CAS](#advanced-compare-and-set-cas).

- `IUrlRepository` file: [`src/Logic/Contracts/IUrlRepository.cs`](src/Logic/Contracts/IUrlRepository.cs)
- `IUrlShortener` file: [`src/Logic/Contracts/IUrlShortener.cs`](src/Logic/Contracts/IUrlShortener.cs)

## Available implementations

### Shorteners

- [`IncrementingIntegerUrlShortener`](src/Logic/Shorteners/IncrementingIntegerUrlShortener.cs) — very fast; unique only during one run.
- [`SnowflakeUrlShortener`](src/Logic/Shorteners/SnowflakeUrlShortener.cs) — time-ordered IDs across machines. Learn about [Snowflake IDs](https://en.wikipedia.org/wiki/Snowflake_ID).
- [`VectorizedUrlShortener`](src/Logic/Shorteners/VectorizedUrlShortener.cs) — uses [SIMD](https://en.wikipedia.org/wiki/SIMD) for speed on supported CPUs.
- [`Murmur3HashUrlShortener`](src/Logic/Shorteners/Murmur3HashUrlShortener.cs) — [MurmurHash3](https://en.wikipedia.org/wiki/MurmurHash) (fast, non-cryptographic).
- [`Sha256HashUrlShortener`](src/Logic/Shorteners/Sha256HashUrlShortener.cs) — [SHA-256](https://en.wikipedia.org/wiki/SHA-2) (cryptographic; slower but stable).

### Repositories

- [`ConcurrentUrlRepository`](src/Logic/Repositories/ConcurrentUrlRepository.cs) — simple, thread-safe baseline using a [concurrent dictionary](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2).
- [`ReaderWriterLockSlimUrlRepository`](src/Logic/Repositories/ReaderWriterLockSlimUrlRepository.cs) — single dictionary with [ReaderWriterLockSlim](https://learn.microsoft.com/dotnet/api/system.threading.readerwriterlockslim).
- [`PartitionedUrlRepository`](src/Logic/Repositories/PartitionedUrlRepository.cs) — multiple dictionaries with shard locks. Learn about [sharding](https://en.wikipedia.org/wiki/Shard_(database)).
- [`MemoryCacheUrlRepository`](src/Logic/Repositories/MemoryCacheUrlRepository.cs) — uses [.NET MemoryCache](https://learn.microsoft.com/aspnet/core/performance/caching/memory) with expiration.
- [`FastCacheUrlRepository`](src/Logic/Repositories/FastCacheUrlRepository.cs) — uses [FastCache](https://www.nuget.org/packages/FastCache/) for fast lookups + expiry.
- [`FASTERUrlRepository`](src/Logic/Repositories/FASTERUrlRepository.cs) — uses [FASTER](https://github.com/microsoft/FASTER) high-throughput key-value store.

Base class for repositories: [`UrlRepositoryBase`](src/Logic/Repositories/UrlRepositoryBase.cs).

## Development setup

From the repo root:

```powershell
# Install dotnet tools
dotnet tool restore

# Check formatting
dotnet format --verify-no-changes

# Strict build (all warnings are elevated to errors)
dotnet build -c Release /p:PedanticMode=true

# Run tests
dotnet test --configuration Release --settings ./build/targets/tests/test.runsettings
```

## Run the console (your own benchmarks)

The console app exposes a `bench` command (see [`BenchmarkCommand`](src/Console/BenchmarkCommand.cs)).

Options:

```text
-t <SHORTENER TYPE>
-s <STORAGE TYPE>
-n <NUMBER OF URLS>
-i <ITERATION COUNT>
-u <URL TO USE>
-p <PARALLELISM DEGREE>
```

Example:

```powershell
dotnet run --project ./src/Console -c Release --framework net9.0 -- bench -t IncrementingIntegerUrlShortener -s ConcurrentUrlRepository -n 5200000000000 -i 3
```

Defaults when you don't pass options:

```text
-t IncrementingIntegerUrlShortener
-s FASTERUrlRepository
-i 3
-n 10000000
-u https://www.microsoft.com
-p 1
```

### Console output

You'll see:

- Iterations and number of URLs processed
- Inserts or reads per second
- Memory usage
- Stats like averages, min/max, and outliers

Use this to compare different shorteners and repositories.

## Advanced: Compare-and-Set (CAS)

When many threads write the same key, a normal save can overwrite someone else's value. [Compare-and-swap (CAS)](https://en.wikipedia.org/wiki/Compare-and-swap) means "only save if the current value is what I expect."

How it works in `IUrlRepository.TrySaveUrlMapping`:

- `true`: store updates safely.
- `false`: store unchanged; you get the actual current value.

Patterns:

- Insert if absent: expected = null
- Update if unchanged: expected = old value
- Retry loop: if fail, read current value and try again

This allows safe updates without heavy locks.

## Benchmarks

We include a [BenchmarkDotNet](https://benchmarkdotnet.org/) project for deeper performance tests.

Run:

```powershell
dotnet run -c Release --framework net9.0 --project ./src/Benchmark/Benchmark.csproj
```

Note: repository benchmarks may take ~20 minutes; shortener benchmarks ~5 minutes.

## Tests

Unit tests use [xUnit](https://xunit.net/). Each implementation of [`IUrlShortener`](src/Logic/Contracts/IUrlShortener.cs) and [`IUrlRepository`](src/Logic/Contracts/IUrlRepository.cs) is covered.

Run tests:

```powershell
dotnet test -c Release --settings ./build/targets/tests/test.runsettings
```

## Troubleshooting the console

- "Unrecognized type 'X'": Run without args to see defaults, or check class names under `src/Logic/Shorteners` and `src/Logic/Repositories`.
- Very slow / high memory: Try `-n 100000 -i 1`.
- Vectorized shortener fails:s Your CPU may not support [AVX2](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#techs=AVX2)/[SSE2](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#techs=SSE2)/[AdvSimd](https://learn.microsoft.com/dotnet/standard/simd). Use Murmur3 or Sha256 instead.

## Glossary

- **Throughput** — operations per second (higher is faster). See [throughput](https://en.wikipedia.org/wiki/Throughput).
- **SIMD** — CPU instructions that process many values at once. See [SIMD](https://en.wikipedia.org/wiki/SIMD).
- **BenchmarkDotNet** — run accurate .NET benchmarks. See [BenchmarkDotNet](https://benchmarkdotnet.org/).
- **Repository** — storage for URL mappings; often a key-value store. See [key-value store](https://en.wikipedia.org/wiki/Key%E2%80%93value_database).
- **Shortener** — generates short codes for URLs. See [URL shortening](https://en.wikipedia.org/wiki/URL_shortening).
- **CAS** — safe update pattern to prevent lost writes. See [compare-and-swap](https://en.wikipedia.org/wiki/Compare-and-swap).
