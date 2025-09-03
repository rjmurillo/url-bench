using System.Diagnostics;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Mathematics;

using Logic.Contracts;

namespace Logic.Benchmarking;

public static class BenchCore
{
    public const int ProgressInterval = 65_536;

    /// <summary>
    /// Executes the write phase of the benchmark sequentially, performing URL shortening and saving operations.
    /// </summary>
    /// <param name="iterations">The number of iterations to perform.</param>
    /// <param name="numberOfUrls">The total number of URLs to process in each iteration.</param>
    /// <param name="url">The URL to be shortened and saved during the benchmark.</param>
    /// <param name="manager">The <see cref="IUrlManager"/> instance responsible for managing URL operations.</param>
    /// <param name="onProgress">
    /// An optional callback invoked periodically during each iteration to report progress.
    /// Parameters include:
    /// <list type="bullet">
    /// <item><description>The current URL index.</description></item>
    /// <item><description>The current iteration number.</description></item>
    /// <item><description>The number of inserts per second.</description></item>
    /// </list>
    /// </param>
    /// <param name="onIterationCompleted">
    /// An optional callback invoked after each iteration is completed.
    /// Parameters include:
    /// <list type="bullet">
    /// <item><description>The completed iteration number.</description></item>
    /// <item><description>The average number of inserts per second for the iteration.</description></item>
    /// <item><description>The elapsed time in milliseconds for the iteration.</description></item>
    /// </list>
    /// </param>
    /// <param name="progressInterval">Optional progress sampling interval; defaults to <see cref="ProgressInterval"/>.</param>
    /// <param name="collector">Optional sample collector which will receive per-window samples to allow scalable statistics.</param>
    /// <param name="minimalOverhead">
    /// If set to true, skips per-interval sampling and progress callbacks,
    /// measuring only iteration totals to reduce overhead.
    /// </param>
    /// <returns>A read-only list of doubles representing the average inserts per second for each iteration.</returns>
    public static IReadOnlyList<double> RunWritePhaseSequential(
        int iterations,
        long numberOfUrls,
        string url,
        IUrlManager manager,
        Action<long, int, double>? onProgress = null,
        Action<int, double, long>? onIterationCompleted = null,
        int progressInterval = ProgressInterval,
        ISampleCollector? collector = null,
        bool minimalOverhead = false)
    {
        var sw = new Stopwatch();
        bool samplingEnabled = !minimalOverhead && (collector is not null || onProgress is not null);
        var results = new List<double>(iterations);

        for (int j = 1; j <= iterations; j++)
        {
            sw.Restart();
            long lastMs = 0;
            if (samplingEnabled)
            {
                for (long i = 0; i < numberOfUrls; i++)
                {
                    manager.ShortenAndSave(url);

                    if (i != 0 && (i % progressInterval) == 0)
                    {
                        long elapsedMs = sw.ElapsedMilliseconds - lastMs;
                        lastMs = sw.ElapsedMilliseconds;
                        if (elapsedMs > 0)
                        {
                            double ips = progressInterval / (elapsedMs / 1000.0);
                            collector?.Add(ips);
                            onProgress?.Invoke(i, j, ips);
                        }
                    }
                }

                // Emit a final remainder sample if there are leftover operations not captured by the exact interval
                long completedAtIntervals = (numberOfUrls / progressInterval) * progressInterval;
                long remaining = numberOfUrls - completedAtIntervals;
                if (remaining > 0)
                {
                    long elapsedMsRemainder = sw.ElapsedMilliseconds - lastMs;
                    if (elapsedMsRemainder > 0)
                    {
                        double ipsRemainder = remaining / (elapsedMsRemainder / 1000.0);
                        collector?.Add(ipsRemainder);
                        onProgress?.Invoke(numberOfUrls, j, ipsRemainder);
                    }
                }
            }
            else
            {
                for (long i = 0; i < numberOfUrls; i++)
                {
                    manager.ShortenAndSave(url);
                }
            }

            sw.Stop();
            double iterIps = sw.ElapsedMilliseconds > 0
                ? numberOfUrls / (sw.ElapsedMilliseconds / 1000.0)
                : double.NaN;
            // Always capture iteration-level result locally
            results.Add(iterIps);
            // Preserve prior behavior of adding iteration aggregate to collector when provided
            collector?.Add(iterIps);
            onIterationCompleted?.Invoke(j, iterIps, sw.ElapsedMilliseconds);
        }

        // If we sampled via collector and minimalOverhead is false, return window-level samples as before; otherwise return iteration-level results
        return (collector is not null && !minimalOverhead) ? collector.Snapshot() : results;
    }

    /// <summary>
    /// Retrieves the original URL corresponding to a given unique identifier.
    /// </summary>
    /// <param name="manager">
    /// An implementation of the <see cref="Logic.Contracts.IUrlManager"/> interface used to manage URL operations.
    /// </param>
    /// <param name="k">
    /// The unique identifier to be encoded and used for retrieving the original URL.
    /// </param>
    /// <remarks>
    /// This method encodes the provided identifier using Base62 encoding and retrieves the original URL
    /// by invoking the <see cref="Logic.Contracts.IUrlManager.GetOriginalUrl(string)"/> method.
    /// It is designed for high-performance scenarios and is inlined for efficiency.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSingle(IUrlManager manager, ulong k)
    {
        int len = Base62Encoder.DetermineBufferLength(k);
        Span<char> slice = stackalloc char[len];
        Base62Encoder.Encode(k, ref slice);

        // Ensure the result is not elided by making it observable
        string result = manager.GetOriginalUrl(slice);

        // Prevents the optimizer from removing the call
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Executes the sequential read phase of the benchmarking process.
    /// </summary>
    /// <param name="iterations">The number of iterations to perform during the read phase.</param>
    /// <param name="numberOfUrls">The total number of URLs to read in each iteration.</param>
    /// <param name="manager">The <see cref="IUrlManager"/> instance responsible for managing URL operations.</param>
    /// <param name="onProgress">
    /// An optional callback invoked periodically during the read phase to report progress.
    /// Parameters include the current URL index, the current iteration number, and the read operations per second (RPS).
    /// </param>
    /// <param name="onIterationCompleted">
    /// An optional callback invoked upon the completion of each iteration.
    /// Parameters include the iteration number, the read operations per second (RPS) for the iteration, and the elapsed time in milliseconds.
    /// </param>
    /// <param name="progressInterval">Optional progress sampling interval; defaults to <see cref="ProgressInterval"/>.</param>
    /// <param name="collector">Optional sample collector which will receive per-window samples to allow scalable statistics.</param>
    /// <param name="minimalOverhead">
    /// If set to true, skips per-interval sampling and progress callbacks,
    /// measuring only iteration totals to reduce overhead.
    /// </param>
    /// <returns>A read-only list of doubles representing the read operations per second (RPS) for each iteration.</returns>
    public static IReadOnlyList<double> RunReadPhaseSequential(
        int iterations,
        long numberOfUrls,
        IUrlManager manager,
        Action<long, int, double>? onProgress = null,
        Action<int, double, long>? onIterationCompleted = null,
        int progressInterval = ProgressInterval,
        ISampleCollector? collector = null,
        bool minimalOverhead = false)
    {
        var rnd = new Random(0);
        var sw = new Stopwatch();
        bool samplingEnabled = !minimalOverhead && (collector is not null || onProgress is not null);
        var results = new List<double>(iterations);

        for (int j = 1; j <= iterations; j++)
        {
            sw.Restart();
            long lastMs = 0;
            if (samplingEnabled)
            {
                for (long i = 0; i < numberOfUrls; i++)
                {
                    ulong k = (ulong)rnd.NextInt64(1, numberOfUrls * j);
                    ReadSingle(manager, k);

                    if (i != 0 && (i % progressInterval) == 0)
                    {
                        long elapsedMs = sw.ElapsedMilliseconds - lastMs;
                        lastMs = sw.ElapsedMilliseconds;
                        if (elapsedMs > 0)
                        {
                            double rps = progressInterval / (elapsedMs / 1000.0);
                            collector?.Add(rps);
                            onProgress?.Invoke(i, j, rps);
                        }
                    }
                }

                // Emit a final remainder sample for reads as well
                long completedAtIntervals = (numberOfUrls / progressInterval) * progressInterval;
                long remaining = numberOfUrls - completedAtIntervals;
                if (remaining > 0)
                {
                    long elapsedMsRemainder = sw.ElapsedMilliseconds - lastMs;
                    if (elapsedMsRemainder > 0)
                    {
                        double rpsRemainder = remaining / (elapsedMsRemainder / 1000.0);
                        collector?.Add(rpsRemainder);
                        onProgress?.Invoke(numberOfUrls, j, rpsRemainder);
                    }
                }
            }
            else
            {
                for (long i = 0; i < numberOfUrls; i++)
                {
                    ulong k = (ulong)rnd.NextInt64(1, numberOfUrls * j);
                    ReadSingle(manager, k);
                }
            }

            sw.Stop();
            double iterRps = sw.ElapsedMilliseconds > 0
                ? numberOfUrls / (sw.ElapsedMilliseconds / 1000.0)
                : double.NaN;
            results.Add(iterRps);
            collector?.Add(iterRps);
            onIterationCompleted?.Invoke(j, iterRps, sw.ElapsedMilliseconds);
        }

        return (collector is not null && !minimalOverhead) ? collector.Snapshot() : results;
    }

    /// <summary>
    /// Computes statistical metrics from a collection of sample values.
    /// </summary>
    /// <param name="samples">A collection of sample values to analyze.
    /// Values that are <see cref="double.NaN"/> or <see cref="double.PositiveInfinity"/>/<see cref="double.NegativeInfinity"/> are excluded from the computation.</param>
    /// <returns>
    /// A <see cref="BenchmarkDotNet.Mathematics.Statistics"/> object containing statistical metrics such as mean, standard deviation, and more.
    /// </returns>
    /// <remarks>
    /// This method is designed to process performance data collected during benchmarking phases,
    /// providing insights into the distribution and variability of the measured samples.
    /// </remarks>
    public static Statistics ComputeStats(IEnumerable<double> samples) => new(samples.Where(p => !double.IsNaN(p) && !double.IsInfinity(p)));
}
