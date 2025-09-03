using System.Diagnostics;
using System.Globalization;
using Logic.Contracts;
using Logic.UI;

namespace Logic.Benchmarking;

/// <summary>
/// Shared benchmark orchestrator used by both Console and AkkaHost to avoid duplication.
/// </summary>
public static class BenchmarkRunner
{
    public static void Run(
        IUrlManager manager,
        int iterations,
        long numberOfUrls,
        string url,
        int maxDegreeOfParallelism,
        int progressInterval,
        int reservoirCapacity,
        int randomSeed,
        bool minimalOverhead)
    {
        if (minimalOverhead || maxDegreeOfParallelism == 1)
        {
            if (minimalOverhead)
            {
                var writeIters = BenchCore.RunWritePhaseSequential(
                    iterations,
                    numberOfUrls,
                    url,
                    manager,
                    minimalOverhead: true);
                var readIters = BenchCore.RunReadPhaseSequential(
                    iterations,
                    numberOfUrls,
                    manager,
                    minimalOverhead: true);

                StatisticsConsoleRenderer.Dump(BenchCore.ComputeStats(writeIters));
                StatisticsConsoleRenderer.Dump(BenchCore.ComputeStats(readIters));
                return;
            }

            // Detailed (sequential) mode with progress and window-level stats
            ISampleCollector writeCollector = new ReservoirSampleCollector(reservoirCapacity, randomSeed);
            var writeIters2 = BenchCore.RunWritePhaseSequential(
                iterations,
                numberOfUrls,
                url,
                manager,
                onProgress: (i, j, ips) =>
                {
                    long workingSet = Process.GetCurrentProcess().WorkingSet64;
                    System.Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Generating... Iteration: {j}, URLs {i:N0}: {workingSet / 1048576}M ({ips:N0} ips)"));
                },
                onIterationCompleted: (j, iterIps, elapsedMs) =>
                {
                    System.Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: upserted {numberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({iterIps:N0} inserts/sec)"));
                },
                progressInterval: progressInterval,
                collector: writeCollector);

            StatisticsConsoleRenderer.Dump(BenchCore.ComputeStats(writeCollector.Snapshot()));
            StatisticsConsoleRenderer.Dump(BenchCore.ComputeStats(writeIters2));

            ISampleCollector readCollector = new ReservoirSampleCollector(reservoirCapacity, randomSeed);
            var readIters2 = BenchCore.RunReadPhaseSequential(
                iterations,
                numberOfUrls,
                manager,
                onProgress: (i, j, rps) =>
                {
                    long workingSet = Process.GetCurrentProcess().WorkingSet64;
                    System.Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Reading... Iteration: {j}, URLs {i:N0}: {workingSet / 1048576}M ({rps:N0} rps)"));
                },
                onIterationCompleted: (j, iterRps, elapsedMs) =>
                {
                    System.Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: read {numberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({iterRps:N0} reads/sec)"));
                },
                progressInterval: progressInterval,
                collector: readCollector);

            StatisticsConsoleRenderer.Dump(BenchCore.ComputeStats(readCollector.Snapshot()));
            StatisticsConsoleRenderer.Dump(BenchCore.ComputeStats(readIters2));
            return;
        }

        // Parallel mode
        Parallel.For(1, iterations + 1, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, j =>
        {
            var swLocal = Stopwatch.StartNew();
            Parallel.For(0L, numberOfUrls, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, _ => manager.ShortenAndSave(url));
            swLocal.Stop();
            var ips = swLocal.ElapsedMilliseconds > 0 ? numberOfUrls / (swLocal.ElapsedMilliseconds / 1000.0) : double.NaN;
            System.Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iter {j}: write {numberOfUrls:N0} in {swLocal.ElapsedMilliseconds / 1000.0:N3}s => {ips:N0} ips"));

            var localRnd = new Random(unchecked(397 ^ j));
            swLocal.Restart();
            for (long i = 0; i < numberOfUrls; i++)
            {
                ulong k = (ulong)localRnd.NextInt64(1, numberOfUrls * j);
                BenchCore.ReadSingle(manager, k);
            }
            swLocal.Stop();
            var rps = swLocal.ElapsedMilliseconds > 0 ? numberOfUrls / (swLocal.ElapsedMilliseconds / 1000.0) : double.NaN;
            System.Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iter {j}: read {numberOfUrls:N0} in {swLocal.ElapsedMilliseconds / 1000.0:N3}s => {rps:N0} rps"));
        });
    }
}
