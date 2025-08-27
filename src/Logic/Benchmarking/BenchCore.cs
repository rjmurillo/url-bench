using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
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

    public readonly record struct ThroughputStats(int Count, double Min, double P50, double P95, double Max, double Mean)
    {
        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"count={Count}, min={Min:N0}, p50={P50:N0}, p95={P95:N0}, max={Max:N0}, mean={Mean:N0}");
    }

    public static ThroughputStats ComputeStats(IEnumerable<double> samples)
    {
        var data = samples
            .Where(p => !double.IsNaN(p) && !double.IsInfinity(p))
            .OrderBy(p => p)
            .ToArray();

        if (data.Length == 0)
        {
            return new ThroughputStats(0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
        }

        static double Percentile(double[] arr, double p)
        {
            if (arr.Length == 0) return double.NaN;
            if (p <= 0) return arr[0];
            if (p >= 1) return arr[^1];
            double pos = (arr.Length - 1) * p;
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            if (lo == hi) return arr[lo];
            double frac = pos - lo;
            return arr[lo] * (1 - frac) + arr[hi] * frac;
        }

        double mean = data.Average();
        double p50 = Percentile(data, 0.5);
        double p95 = Percentile(data, 0.95);

        return new ThroughputStats(data.Length, data[0], p50, p95, data[^1], mean);
    }
}
