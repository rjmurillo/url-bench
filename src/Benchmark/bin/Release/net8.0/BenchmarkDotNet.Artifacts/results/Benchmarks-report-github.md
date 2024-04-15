```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3374/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900K, 1 CPU, 32 logical and 24 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host] : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2

Job=Net80  Runtime=.NET 8.0  Server=True  
Toolchain=InProcessEmitToolchain  IterationCount=1  LaunchCount=2  
RunStrategy=Throughput  WarmupCount=10  

```
| Method           | ShortenerType              | RepositoryType                               | LongUrl              | Mean     | Error | StdDev   | Gen0   | Gen1   | Allocated |
|----------------- |--------------------------- |--------------------------------------------- |--------------------- |---------:|------:|---------:|-------:|-------:|----------:|
| Shorten          | ReadOnlyMemoryUrlShortener | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] | 336.9 ns |    NA |  6.67 ns | 0.0038 | 0.0033 |      72 B |
| Shorten_Memory   | ReadOnlyMemoryUrlShortener | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] | 279.0 ns |    NA |  8.47 ns | 0.0019 | 0.0014 |      40 B |
| RoundTrip        | ReadOnlyMemoryUrlShortener | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] | 410.3 ns |    NA | 34.28 ns | 0.0105 | 0.0052 |     200 B |
| RoundTrip_Memory | ReadOnlyMemoryUrlShortener | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] | 361.8 ns |    NA | 17.94 ns | 0.0038 | 0.0033 |      80 B |
