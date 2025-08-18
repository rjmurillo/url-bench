# URL Bench

**URL Bench** is a set of benchmarks built around two interfaces:

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

There are implementations of each utilizing various .NET features and capabilities, as well as popular libraries.
The code is split into the following:

- `src/Logic/Contracts` = The contracts above used to implement all shorteners and repositories
- `src/Logic` = The implementation of various contracts
  - Shorteners
    - `IncrementingInteger` making use of `Interlocked.Increment`
    - `Murmur3Hash` making use of the non-cryptographic [MurmurHash](https://en.wikipedia.org/wiki/MurmurHash)
    - `Sha256` making use of the cryptographic SHA-256 hash.
    - `Snowflake` making use of [Snowflake ID](https://en.wikipedia.org/wiki/Snowflake_ID)
    - `Vectorized` non-cryptographic hashing making use of hardware accelerated AVX, SSE2, or ARM SIMD
  - Repositories
    - `Concurrent` making use of the built-in .NET `ConcurrentDictionary<string, string>` type
    - `FastCache` making use of [fast-cache](https://github.com/neon-sunset/fast-cache) to include items with expiration time
    - `FASTER` using a [concurrent key-value store + cache](https://aka.ms/faster)
    - `IMemoryCache` using [ASP.NET Core's built in memory-based cache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-8.0)
    - `Partitioned` using a naive array of `Dictionary<string,string>` and `ReaderWriterLockSlim`.
    - `ReaderWriterLockSlim` using a single `Dictionary<string,string>` and `ReaderWriterLockSlim`
- `src/Console` = Console application for running throughput benchmarks
- `src/Benchmark` = BenchmarkDotNet project to benchmark shorteners and repositories
- `test` = A suite of tests for the shorteners and repositories

## Development Setup

This repository targets modern .NET SDKs (see `global.json` for the exact version). Ensure the required SDK is installed before building and running benchmarks.

## Prerequisites

- Operating system: Windows, macOS, or Linux
- Git: [git-scm.com/downloads](https://git-scm.com/downloads)
- .NET SDK 9: [dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

Verify your install:

```pwsh
dotnet --info
git --version
```

The SDK version is pinned by `global.json` (9.0.304). Install that version or later within .NET 9.

## Build and test

Prerequisite: .NET SDK 9.0.304 (as pinned in `global.json`).

From the repository root:

```pwsh
# Build (Release)
dotnet build -c Release

# Optional: enable stricter build
dotnet build -c Release /p:PedanticMode=true

# Check formatting
dotnet format --verify-no-changes

# Run unit tests (Release)
dotnet test -c Release
```

## Quick start (5 minutes)

Run a small, safe benchmark to see the app working end-to-end.

```pwsh
# From the repo root
dotnet run --project ./src/Console -- bench -n 10000 -i 1 -t IncrementingIntegerUrlShortener -s ConcurrentUrlRepository
```

What this does

- Builds and runs the console app.
- Shortens and stores 10,000 URLs one time (i = 1).
- Prints how many inserts/reads per second it achieved and shows basic stats.

## Key terms

- Runtime Identifier (RID): A name that tells .NET what OS/CPU you target. See the [RID catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog).
- Publish: Create files to run your app on another machine. See [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).
- Self-contained: A published app that includes the .NET runtime so users do not need to install .NET. See [deploying apps](https://learn.microsoft.com/en-us/dotnet/core/deploying/#publish-framework-dependent-or-self-contained).
- Single-file: Publish your app as one executable file. See [single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).
- BenchmarkDotNet: A library that runs performance tests the right way. See [benchmarkdotnet.org](https://benchmarkdotnet.org/).
- SIMD / vectorization: CPU features that process many values in one step. See [SIMD in .NET](https://learn.microsoft.com/en-us/dotnet/standard/simd) and [hardware intrinsics](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics?view=net-9.0).
  - AVX2 (x86/x64): [System.Runtime.Intrinsics.X86.Avx2](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.x86.avx2?view=net-9.0)
  - SSE2 (x86/x64): [System.Runtime.Intrinsics.X86.Sse2](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.x86.sse2?view=net-9.0)
  - AdvSimd (ARM/ARM64): [System.Runtime.Intrinsics.Arm.AdvSimd](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.arm.advsimd?view=net-9.0)
- Throughput: How many operations happen per second (higher is better).
- Working set: The process memory currently in use. See [.NET Process.WorkingSet64](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.workingset64?view=net-9.0).

## Running with console

To run the console, it is recommended that you run `dotnet publish` with the runtime identifier needed as a single file and self contained.

```sh
dotnet publish -r linux-arm64 -p:PublishSingleFile=true --self-contained true -c Release
```

Publish output will be placed under `src/Console/bin/Release/net8.0/linux-arm64/publish/` (or the corresponding RID/framework).

> NOTE: There is a script next to the `Console.csproj` called `publish.cmd` that will publish common RIDs

The console program has options:

```sh
-t <SHORTENER TYPE>
-i <ITERATION COUNT>
-n <NUMBER OF URLS>
-u <URL TO USE FOR BENCHMARK>
-s <STORAGE TYPE>
-p <DEGREES OF PARALLELISM>
```

### Example: run with ConcurrentDictionary-based storage

Running the console (Vectorized shortener + Concurrent repository):

```sh
./Console bench -t VectorizedUrlShortener -s ConcurrentUrlRepository
# or on Windows (from publish folder):
./Console.exe bench -t VectorizedUrlShortener -s ConcurrentUrlRepository
```

### Console Output

If no options are specified, the defaults are the following:

```text
-t IncrementingIntegerUrlShortener
-s FASTERUrlRepository
-i 3
-n 10000000
-u https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint
-p 1
```

Output of Console is similar to the following:

```sh
╭────────────────────────┬────────────────────────────────────────────────────────────────────────────────────────────╮
│ Name                   │ Value                                                                                      │
├────────────────────────┼────────────────────────────────────────────────────────────────────────────────────────────┤
│ Iterations             │ 3                                                                                          │
│ NumberOfUrls           │ 10000000                                                                                   │
│ Url                    │ https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-end │
│                        │ point                                                                                      │
│ StorageType            │ FASTERUrlRepository                                                                        │
│ ShortenerType          │ IncrementingIntegerUrlShortener                                                            │
│ MaxDegreeOfParallelism │ 1                                                                                          │
╰────────────────────────┴────────────────────────────────────────────────────────────────────────────────────────────╯
Iteration 1: Total time to upsert 10,000,000 elements: 0.012 secs (833,333,333 inserts/sec)
Iteration 2: Total time to upsert 10,000,000 elements: 0.016 secs (625,000,000 inserts/sec)
Iteration 3: Total time to upsert 10,000,000 elements: 0.023 secs (434,782,609 inserts/sec)
URLs: 30000000 Working set: 1994M
╭────────────────────┬────────────────────────────────────────────────────────────────────────────────────────────────╮
│ Name               │ Value                                                                                          │
├────────────────────┼────────────────────────────────────────────────────────────────────────────────────────────────┤
│ OriginalValues     │ 2,500,000,000, 29,498,525, 76,923,077, 69,444,444, 75,187,970, 73,529,412, 67,567,568,         │
│                    │ 71,942,446, 67,567,568, 72,992,701, 71,942,446, 64,935,065, 66,225,166, 67,114,094,            │
│                    │ 64,102,564, 65,789,474, 60,606,061, 62,111,801, 59,171,598, 55,248,619, 833,333,333,           │
│                    │ 59,171,598, 58,139,535, 53,763,441, 55,865,922, 52,356,021, 51,020,408, 51,813,472,            │
│                    │ 47,846,890, 49,751,244, 49,751,244, 46,511,628, 46,511,628, 44,642,857, 45,871,560,            │
│                    │ 44,843,049, 43,290,043, 44,642,857, 42,918,455, 41,493,776, 625,000,000, 42,735,043,           │
│                    │ 40,000,000, 41,666,667, 41,322,314, 38,167,939, 39,840,637, 39,370,079, 36,496,350,            │
│                    │ 37,878,788, 36,231,884, 37,174,721, 36,900,369, 34,722,222, 34,965,035, 34,843,206,            │
│                    │ 33,557,047, 34,129,693, 32,573,290, 33,557,047, 434,782,609                                    │
│ N                  │ 61                                                                                             │
│ Min                │ 29,498,525                                                                                     │
│ LowerFence         │ 917,383                                                                                        │
│ Q1                 │ 39,840,637                                                                                     │
│ Median             │ 49,751,244                                                                                     │
│ Mean               │ 119,202,533                                                                                    │
│ Q3                 │ 65,789,474                                                                                     │
│ UpperFence         │ 104,712,728                                                                                    │
│ Max                │ 2,500,000,000                                                                                  │
│ InterquartileRange │ 25,948,836                                                                                     │
│ LowerOutliers      │                                                                                                │
│ UpperOutliers      │ 434,782,609, 625,000,000, 833,333,333, 2,500,000,000                                           │
│ AllOutliers        │ 434,782,609, 625,000,000, 833,333,333, 2,500,000,000                                           │
│ StandardError      │ 43,144,453                                                                                     │
│ Variance           │ 113,548,072,919,640,480                                                                        │
│ StandardDeviation  │ 336,968,949                                                                                    │
│ Skewness           │ 6                                                                                              │
│ Kurtosis           │ 41                                                                                             │
│ ConfidenceInterval │ [-30085934.62; 268491000.05] (CI 99.9%)                                                        │
│ Percentiles        │ [P95: 434782608.7]; [P0: 29498525.07]; [P50: 49751243.78]; [P100: 2500000000]                  │
╰────────────────────┴────────────────────────────────────────────────────────────────────────────────────────────────╯
Mean = 119,202,533, StdErr = 43,144,453 (36.19%), N = 61, StdDev = 336,968,949
Min = 29,498,525, Q1 = 39,840,637, Median = 49,751,244, Q3 = 65,789,474, Max = 2,500,000,000
IQR = 25,948,836, LowerFence = 917,383, UpperFence = 104,712,728
ConfidenceInterval = [-30,085,935; 268,491,000] (CI 99.9%), Margin = 149,288,467 (125.24% of Mean)
Skewness = 6, Kurtosis = 41.28, MValue = 2.63
-------------------- Histogram --------------------
[  -59,140,796 ;   165,562,398) | @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
[  165,562,398 ;   417,539,707) |
[  417,539,707 ;   642,242,902) | @@
[  642,242,902 ;   720,981,736) |
[  720,981,736 ;   945,684,931) | @
[  945,684,931 ; 1,170,388,125) |
[1,170,388,125 ; 1,395,091,320) |
[1,395,091,320 ; 1,619,794,514) |
[1,619,794,514 ; 1,844,497,709) |
[1,844,497,709 ; 2,069,200,903) |
[2,069,200,903 ; 2,293,904,098) |
[2,293,904,098 ; 2,387,648,403) |
[2,387,648,403 ; 2,612,351,597) | @
---------------------------------------------------
4 outliers were removed (5,032.2061 d..28,935.1852 d)
╭────────────────────┬────────────────────────────────────────────────────────────────────────────────────────────────╮
│ Name               │ Value                                                                                          │
├────────────────────┼────────────────────────────────────────────────────────────────────────────────────────────────┤
│ OriginalValues     │ 29,498,525, 76,923,077, 69,444,444, 75,187,970, 73,529,412, 67,567,568, 71,942,446,            │
│                    │ 72,992,701, 64,935,065, 66,225,166, 67,114,094, 64,102,564, 65,789,474, 60,606,061,            │
│                    │ 62,111,801, 59,171,598, 55,248,619, 58,139,535, 53,763,441, 55,865,922, 52,356,021,            │
│                    │ 51,020,408, 51,813,472, 47,846,890, 49,751,244, 46,511,628, 44,642,857, 45,871,560,            │
│                    │ 44,843,049, 43,290,043, 42,918,455, 41,493,776, 42,735,043, 40,000,000, 41,666,667,            │
│                    │ 41,322,314, 38,167,939, 39,840,637, 39,370,079, 36,496,350, 37,878,788, 36,231,884,            │
│                    │ 37,174,721, 36,900,369, 34,722,222, 34,965,035, 34,843,206, 33,557,047, 34,129,693, 32,573,290 │
│ N                  │ 50                                                                                             │
│ Min                │ 29,498,525                                                                                     │
│ LowerFence         │ 3,568,136                                                                                      │
│ Q1                 │ 38,468,474                                                                                     │
│ Median             │ 46,191,594                                                                                     │
│ Mean               │ 50,101,883                                                                                     │
│ Q3                 │ 61,735,366                                                                                     │
│ UpperFence         │ 96,635,704                                                                                     │
│ Max                │ 76,923,077                                                                                     │
│ InterquartileRange │ 23,266,892                                                                                     │
│ LowerOutliers      │                                                                                                │
│ UpperOutliers      │                                                                                                │
│ AllOutliers        │                                                                                                │
│ StandardError      │ 1,920,141                                                                                      │
│ Variance           │ 184,346,996,262,311                                                                            │
│ StandardDeviation  │ 13,577,444                                                                                     │
│ Skewness           │ 0                                                                                              │
│ Kurtosis           │ 2                                                                                              │
│ ConfidenceInterval │ [43380540.31; 56823226.35] (CI 99.9%)                                                          │
│ Percentiles        │ [P95: 73287891.8]; [P0: 29498525.07]; [P50: 46191593.77]; [P100: 76923076.92]                  │
╰────────────────────┴────────────────────────────────────────────────────────────────────────────────────────────────╯
Mean = 50,101,883, StdErr = 1,920,141 (3.83%), N = 50, StdDev = 13,577,444
Min = 29,498,525, Q1 = 38,468,474, Median = 46,191,594, Q3 = 61,735,366, Max = 76,923,077
IQR = 23,266,892, LowerFence = 3,568,136, UpperFence = 96,635,704
ConfidenceInterval = [43,380,540; 56,823,226] (CI 99.9%), Margin = 6,721,343 (13.42% of Mean)
Skewness = 0.42, Kurtosis = 1.83, MValue = 2.42
-------------------- Histogram --------------------
[24,661,325 ; 33,872,668) | @@@
[33,872,668 ; 43,547,068) | @@@@@@@@@@@@@@@@@@@
[43,547,068 ; 54,040,349) | @@@@@@@@@@
[54,040,349 ; 63,978,788) | @@@@@@
[63,978,788 ; 73,653,188) | @@@@@@@@@@
[73,653,188 ; 81,760,277) | @@
---------------------------------------------------
Iteration 1: Total time to read 10,000,000 elements: 0.022 secs (454,545,455 reads/sec)
Iteration 2: Total time to read 10,000,000 elements: 0.027 secs (370,370,370 reads/sec)
Iteration 3: Total time to read 10,000,000 elements: 0.035 secs (285,714,286 reads/sec)
URLs: 30000000 Working set: 2020M
╭────────────────────┬────────────────────────────────────────────────────────────────────────────────────────────────╮
│ Name               │ Value                                                                                          │
├────────────────────┼────────────────────────────────────────────────────────────────────────────────────────────────┤
│ OriginalValues     │ 5,000,000,000, 26,666,667, 35,971,223, 33,444,816, 35,460,993, 35,335,689, 35,460,993,         │
│                    │ 35,971,223, 34,364,261, 36,101,083, 34,602,076, 35,971,223, 36,101,083, 35,714,286,            │
│                    │ 36,630,037, 35,460,993, 35,971,223, 33,112,583, 35,087,719, 34,364,261, 454,545,455,           │
│                    │ 27,173,913, 28,490,028, 24,154,589, 27,100,271, 26,041,667, 28,571,429, 25,445,293,            │
│                    │ 26,246,719, 25,125,628, 25,380,711, 22,988,506, 24,449,878, 25,188,917, 24,630,542,            │
│                    │ 25,773,196, 27,247,956, 23,529,412, 26,246,719, 24,752,475, 370,370,370, 22,421,525,           │
│                    │ 22,371,365, 22,075,055, 22,471,910, 21,459,227, 21,739,130, 22,573,363, 21,141,649,            │
│                    │ 22,831,050, 21,551,724, 23,866,348, 21,691,974, 23,923,445, 22,624,434, 23,255,814,            │
│                    │ 22,675,737, 22,471,910, 22,026,432, 20,833,333, 285,714,286                                    │
│ N                  │ 61                                                                                             │
│ Min                │ 20,833,333                                                                                     │
│ LowerFence         │ 4,074,092                                                                                      │
│ Q1                 │ 22,831,050                                                                                     │
│ Median             │ 26,041,667                                                                                     │
│ Mean               │ 125,983,046                                                                                    │
│ Q3                 │ 35,335,689                                                                                     │
│ UpperFence         │ 54,092,647                                                                                     │
│ Max                │ 5,000,000,000                                                                                  │
│ InterquartileRange │ 12,504,639                                                                                     │
│ LowerOutliers      │                                                                                                │
│ UpperOutliers      │ 285,714,286, 370,370,370, 454,545,455, 5,000,000,000                                           │
│ AllOutliers        │ 285,714,286, 370,370,370, 454,545,455, 5,000,000,000                                           │
│ StandardError      │ 81,820,906                                                                                     │
│ Variance           │ 408,374,300,511,167,040                                                                        │
│ StandardDeviation  │ 639,041,705                                                                                    │
│ Skewness           │ 7                                                                                              │
│ Kurtosis           │ 55                                                                                             │
│ ConfidenceInterval │ [-157133712.25; 409099804.64] (CI 99.9%)                                                       │
│ Percentiles        │ [P95: 285714285.71]; [P0: 20833333.33]; [P50: 26041666.67]; [P100: 5000000000]                 │
╰────────────────────┴────────────────────────────────────────────────────────────────────────────────────────────────╯
Mean = 125,983,046, StdErr = 81,820,906 (64.95%), N = 61, StdDev = 639,041,705
Min = 20,833,333, Q1 = 22,831,050, Median = 26,041,667, Q3 = 35,335,689, Max = 5,000,000,000
IQR = 12,504,639, LowerFence = 4,074,092, UpperFence = 54,092,647
ConfidenceInterval = [-157,133,712; 409,099,805] (CI 99.9%), Margin = 283,116,758 (224.73% of Mean)
Skewness = 7.27, Kurtosis = 55.48, MValue = 3.44
-------------------- Histogram --------------------
[  -17,466,315 ;   408,670,018) | @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
[  408,670,018 ;   667,613,621) | @
[  667,613,621 ; 1,093,749,954) |
[1,093,749,954 ; 1,519,886,287) |
[1,519,886,287 ; 1,946,022,621) |
[1,946,022,621 ; 2,372,158,954) |
[2,372,158,954 ; 2,798,295,287) |
[2,798,295,287 ; 3,224,431,620) |
[3,224,431,620 ; 3,650,567,953) |
[3,650,567,953 ; 4,076,704,287) |
[4,076,704,287 ; 4,502,840,620) |
[4,502,840,620 ; 4,786,931,833) |
[4,786,931,833 ; 5,213,068,167) | @
---------------------------------------------------
4 outliers were removed (3,306.8783 d..57,870.3704 d)
╭────────────────────┬────────────────────────────────────────────────────────────────────────────────────────────────╮
│ Name               │ Value                                                                                          │
├────────────────────┼────────────────────────────────────────────────────────────────────────────────────────────────┤
│ OriginalValues     │ 26,666,667, 35,971,223, 33,444,816, 35,460,993, 35,335,689, 34,364,261, 36,101,083,            │
│                    │ 34,602,076, 35,714,286, 36,630,037, 33,112,583, 35,087,719, 27,173,913, 28,490,028,            │
│                    │ 24,154,589, 27,100,271, 26,041,667, 28,571,429, 25,445,293, 26,246,719, 25,125,628,            │
│                    │ 25,380,711, 22,988,506, 24,449,878, 25,188,917, 24,630,542, 25,773,196, 27,247,956,            │
│                    │ 23,529,412, 24,752,475, 22,421,525, 22,371,365, 22,075,055, 22,471,910, 21,459,227,            │
│                    │ 21,739,130, 22,573,363, 21,141,649, 22,831,050, 21,551,724, 23,866,348, 21,691,974,            │
│                    │ 23,923,445, 22,624,434, 23,255,814, 22,675,737, 22,026,432, 20,833,333                         │
│ N                  │ 48                                                                                             │
│ Min                │ 20,833,333                                                                                     │
│ LowerFence         │ 13,763,599                                                                                     │
│ Q1                 │ 22,611,667                                                                                     │
│ Median             │ 24,939,052                                                                                     │
│ Mean               │ 26,589,918                                                                                     │
│ Q3                 │ 28,510,379                                                                                     │
│ UpperFence         │ 37,358,446                                                                                     │
│ Max                │ 36,630,037                                                                                     │
│ InterquartileRange │ 5,898,712                                                                                      │
│ LowerOutliers      │                                                                                                │
│ UpperOutliers      │                                                                                                │
│ AllOutliers        │                                                                                                │
│ StandardError      │ 730,249                                                                                        │
│ Variance           │ 25,596,619,493,162                                                                             │
│ StandardDeviation  │ 5,059,310                                                                                      │
│ Skewness           │ 1                                                                                              │
│ Kurtosis           │ 2                                                                                              │
│ ConfidenceInterval │ [24026817.88; 29153018.73] (CI 99.9%)                                                          │
│ Percentiles        │ [P95: 35881294.96]; [P0: 20833333.33]; [P50: 24939051.69]; [P100: 36630036.63]                 │
╰────────────────────┴────────────────────────────────────────────────────────────────────────────────────────────────╯
Mean = 26,589,918, StdErr = 730,249 (2.75%), N = 48, StdDev = 5,059,310
Min = 20,833,333, Q1 = 22,611,667, Median = 24,939,052, Q3 = 28,510,379, Max = 36,630,037
IQR = 5,898,712, LowerFence = 13,763,599, UpperFence = 37,358,446
ConfidenceInterval = [24,026,818; 29,153,019] (CI 99.9%), Margin = 2,563,100 (9.64% of Mean)
Skewness = 0.86, Kurtosis = 2.22, MValue = 2.96
-------------------- Histogram --------------------
[19,006,172 ; 21,119,901) | @
[21,119,901 ; 24,774,223) | @@@@@@@@@@@@@@@@@@@@@@@
[24,774,223 ; 28,675,690) | @@@@@@@@@@@@@
[28,675,690 ; 33,044,148) |
[33,044,148 ; 36,698,471) | @@@@@@@@@@@
---------------------------------------------------
```

## Running with Benchmark

Also included is a BenchmarkDotNet suite that takes about 20 minutes to run for each batch of shortener and repository.

```sh
dotnet run -c Release --framework net8.0 --project ./src/Benchmark/Benchmark.csproj
```

Various benchmarks are also provided to test capabilities of different types of shorteners and repositories in throughput and multi-threaded scenarios.
This will give the menu to select which set of bencharks to run (Repository or Shortener, or both). Repository takes about 20 minutes. Shortener takes about five minutes.

## Tests

Unit tests are provided for the shorteners and repositories through xUnit's `TheoryData<,>` construct: each implementation of `IUrlShortener` and `IUrlRepository` is automatically detected and placed into respective theory-based tests.