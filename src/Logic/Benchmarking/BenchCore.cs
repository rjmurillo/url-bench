using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Mathematics;

using Logic.Contracts;

namespace Logic.Benchmarking;

/// <summary>
/// Provides core benchmarking functionality for measuring the performance of URL shortening and retrieval operations.
/// </summary>
/// <remarks>
/// This class contains methods to execute sequential write and read benchmarking phases, as well as utilities to compute statistical results.
/// It is designed to work with an <see cref="Logic.Contracts.IUrlManager"/> implementation for managing URL operations.
/// </remarks>
public static class BenchCore
{
    public const int ProgressInterval = 524_288;

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
    /// <returns>A read-only list of doubles representing the average inserts per second for each iteration.</returns>
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
        Span<char> buf = stackalloc char[12];
        int len = Base62Encoder.DetermineBufferLength(k);
        Span<char> slice = buf[..len];
        Base62Encoder.Encode(k, ref slice);
        string result = manager.GetOriginalUrl(slice);

        // Ensure the result is not elided by making it observable
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
    /// <returns>A read-only list of doubles representing the read operations per second (RPS) for each iteration.</returns>
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
    public static Statistics ComputeStats(IEnumerable<double> samples)
    {
        return new Statistics(samples.Where(p => !double.IsNaN(p) && !double.IsInfinity(p)));
    }
}
