using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Mathematics;

using Logic.Contracts;

namespace Logic.Benchmarking;

public static class BenchCore
{
    public const int ProgressInterval = 524_288;

    public static IReadOnlyList<double> RunWritePhaseSequential(
        int iterations,
        long numberOfUrls,
        string url,
        IUrlManager manager,
        Action<long, int, double>? onProgress = null,
        Action<int, double, long>? onIterationCompleted = null)
    {
        var sw = new Stopwatch();
        var samples = new List<double>(iterations);

        for (int j = 1; j <= iterations; j++)
        {
            sw.Restart();
            long lastMs = 0;
            for (long i = 0; i < numberOfUrls; i++)
            {
                manager.ShortenAndSave(url);

                if (i != 0 && (i % ProgressInterval) == 0)
                {
                    long elapsedMs = sw.ElapsedMilliseconds - lastMs;
                    lastMs = sw.ElapsedMilliseconds;
                    if (elapsedMs > 0)
                    {
                        double ips = ProgressInterval / (elapsedMs / 1000.0);
                        onProgress?.Invoke(i, j, ips);
                    }
                }
            }

            sw.Stop();
            double iterIps = sw.ElapsedMilliseconds > 0
                ? numberOfUrls / (sw.ElapsedMilliseconds / 1000.0)
                : double.NaN;
            samples.Add(iterIps);
            onIterationCompleted?.Invoke(j, iterIps, sw.ElapsedMilliseconds);
        }

        return samples;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReadSingle(IUrlManager facade, ulong k)
    {
        Span<char> buf = stackalloc char[12];
        int len = Base62Encoder.DetermineBufferLength(k);
        Span<char> slice = buf[..len];
        Base62Encoder.Encode(k, ref slice);
        _ = facade.GetOriginalUrl(slice);
    }

    public static IReadOnlyList<double> RunReadPhaseSequential(
        int iterations,
        long numberOfUrls,
        IUrlManager manager,
        Action<long, int, double>? onProgress = null,
        Action<int, double, long>? onIterationCompleted = null)
    {
        var rnd = new Random(0);
        var sw = new Stopwatch();
        var samples = new List<double>(iterations);

        for (int j = 1; j <= iterations; j++)
        {
            sw.Restart();
            long lastMs = 0;
            for (long i = 0; i < numberOfUrls; i++)
            {
                ulong k = (ulong)rnd.NextInt64(1, numberOfUrls * j);
                ReadSingle(manager, k);

                if (i != 0 && (i % ProgressInterval) == 0)
                {
                    long elapsedMs = sw.ElapsedMilliseconds - lastMs;
                    lastMs = sw.ElapsedMilliseconds;
                    if (elapsedMs > 0)
                    {
                        double rps = ProgressInterval / (elapsedMs / 1000.0);
                        onProgress?.Invoke(i, j, rps);
                    }
                }
            }

            sw.Stop();
            double iterRps = sw.ElapsedMilliseconds > 0
                ? numberOfUrls / (sw.ElapsedMilliseconds / 1000.0)
                : double.NaN;
            samples.Add(iterRps);
            onIterationCompleted?.Invoke(j, iterRps, sw.ElapsedMilliseconds);
        }

        return samples;
    }

    public static Statistics ComputeStats(IEnumerable<double> samples)
    {
        return new Statistics(samples.Where(p => !double.IsNaN(p) && !double.IsInfinity(p)));
    }
}
