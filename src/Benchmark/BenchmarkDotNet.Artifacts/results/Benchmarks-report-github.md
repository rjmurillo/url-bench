```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3374/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900K, 1 CPU, 32 logical and 24 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host] : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2

Job=Net90  Runtime=.NET 8.0  Server=True  
Toolchain=InProcessEmitToolchain  IterationCount=3  LaunchCount=2  
RunStrategy=Throughput  WarmupCount=10  

```
| Method           | ShortenerType | RepositoryType                               | LongUrl              | Mean       | Error      | StdDev     | Gen0   | Gen1   | Allocated |
|----------------- |-------------- |--------------------------------------------- |--------------------- |-----------:|-----------:|-----------:|-------:|-------:|----------:|
| **Shorten**          | **UrlShortener**  | **ReaderWriterLockSlimReadOnlyMemoryRepository** | **https(...)13473 [52]** | **285.602 ns** | **39.1676 ns** | **13.9676 ns** | **0.0014** | **0.0010** |      **32 B** |
| Shorten_Memory   | UrlShortener  | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] |   1.434 ns |  0.0517 ns |  0.0185 ns |      - |      - |         - |
| RoundTrip        | UrlShortener  | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] | 376.765 ns | 59.3714 ns | 21.1724 ns | 0.0081 | 0.0038 |     160 B |
| RoundTrip_Memory | UrlShortener  | ReaderWriterLockSlimReadOnlyMemoryRepository | https(...)13473 [52] |   1.410 ns |  0.0286 ns |  0.0102 ns |      - |      - |         - |
| **Shorten**          | **UrlShortener**  | **ReaderWriterLockSlimUrlRepository**            | **https(...)13473 [52]** | **173.288 ns** | **18.1725 ns** |  **6.4805 ns** | **0.0014** | **0.0010** |      **32 B** |
| Shorten_Memory   | UrlShortener  | ReaderWriterLockSlimUrlRepository            | https(...)13473 [52] |   1.422 ns |  0.0646 ns |  0.0230 ns |      - |      - |         - |
| RoundTrip        | UrlShortener  | ReaderWriterLockSlimUrlRepository            | https(...)13473 [52] | 197.656 ns | 26.6033 ns |  9.4870 ns | 0.0014 | 0.0010 |      32 B |
| RoundTrip_Memory | UrlShortener  | ReaderWriterLockSlimUrlRepository            | https(...)13473 [52] |   1.470 ns |  0.0494 ns |  0.0176 ns |      - |      - |         - |
