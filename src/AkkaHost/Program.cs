using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using Akka.Actor;
using Akka.Hosting;
using AkkaHost;
using Logic.Benchmarking;
using Logic.Contracts;
using Logic.Repositories;
using Logic.Shorteners;
using Logic.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Bump ThreadPool minimum threads to reduce ramp-up stalls under load
ThreadPool.GetMinThreads(out var minWorkers, out var minIoThreads);
var targetWorkers = Math.Max(minWorkers, Environment.ProcessorCount * 4);
ThreadPool.SetMinThreads(targetWorkers, minIoThreads);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Provide a shortener and repository
        services.AddSingleton<IUrlShortener, IncrementingIntegerUrlShortener>();
        services.AddSingleton<IUrlRepository, ConcurrentUrlRepository>();

        services.AddAkka("UrlShortenerSystem", (builder, sp) =>
        {
            // Disable verbose logging, dead-letters, and increase dispatcher throughput
            builder.AddHocon(@"akka {
              log-config-on-start = off
              loglevel = ERROR
              stdout-loglevel = ERROR
              log-dead-letters = off
              log-dead-letters-during-shutdown = off
              actor {
                debug {
                  receive = off
                  autoreceive = off
                  lifecycle = off
                  event-stream = off
                  unhandled = off
                }
                default-dispatcher {
                  # Larger throughput reduces scheduler context switching
                  throughput = 1000
                }
              }
            }", HoconAddMode.Prepend);

            // No persistence plugins configured - we are in-memory only
            builder.WithActors((system, registry) =>
            {
                var shortener = sp.GetRequiredService<IUrlShortener>();
                var repo = sp.GetRequiredService<IUrlRepository>();

                var writer = system.ActorOf(Props.Create(() => new UrlWriteActor(shortener, repo)), "writer");
                var reader = system.ActorOf(Props.Create(() => new UrlReadActor(repo)), "reader");

                registry.Register<UrlWriteActor>(writer);
                registry.Register<UrlReadActor>(reader);
            });
        });
    })
    .Build();

await host.StartAsync().ConfigureAwait(false);

// Bench mode (hard-coded defaults)
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

const string Url = "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";
const long NumberOfUrls = 1_000_000L;
const int Iterations = 1;
const int ProgressInterval = BenchCore.ProgressInterval;

var registry = host.Services.GetRequiredService<ActorRegistry>();
var writerRef = registry.Get<UrlWriteActor>();
var readerRef = registry.Get<UrlReadActor>();

IUrlManager manager = new AkkaUrlManager(writerRef, readerRef);

Console.WriteLine("Starting benchmark (sequential)...");

ISampleCollector writeCollector = new ReservoirSampleCollector(short.MaxValue, 0);
var writeSamples = BenchCore.RunWritePhaseSequential(
    Iterations,
    NumberOfUrls,
    Url,
    manager,
    onProgress: (i, j, ips) =>
    {
        long workingSet = Process.GetCurrentProcess().WorkingSet64;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Progress: write iter {j}, at {i:N0}, {ips:N0} inserts/sec, Mem: {workingSet / 1048576}M"));
    },
    onIterationCompleted: (j, ips, elapsedMs) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: upserted {NumberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({ips:N0} inserts/sec)")),
    progressInterval: ProgressInterval,
    collector: writeCollector);

long workingSetW = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Write complete. Total URLs: {NumberOfUrls * Iterations:N0} Working set: {workingSetW / 1048576}M"));
var writeWindowStats = BenchCore.ComputeStats(writeCollector.Snapshot());
StatisticsConsoleRenderer.Dump(writeWindowStats);
var writeStats = BenchCore.ComputeStats(writeSamples);
StatisticsConsoleRenderer.Dump(writeStats);

ISampleCollector readCollector = new ReservoirSampleCollector(short.MaxValue, 0);
var readSamples = BenchCore.RunReadPhaseSequential(
    Iterations,
    NumberOfUrls,
    manager,
    onProgress: (i, j, rps) =>
    {
        long workingSet = Process.GetCurrentProcess().WorkingSet64;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Progress: read iter {j}, at {i:N0}, {rps:N0} reads/sec, Mem: {workingSet / 1048576}M"));
    },
    onIterationCompleted: (j, rps, elapsedMs) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: read {NumberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({rps:N0} reads/sec)")),
    progressInterval: ProgressInterval,
    collector: readCollector);

long workingSetR = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Read complete. Total URLs: {NumberOfUrls * Iterations:N0} Working set: {workingSetR / 1048576}M"));
var readWindowStats = BenchCore.ComputeStats(readCollector.Snapshot());
StatisticsConsoleRenderer.Dump(readWindowStats);
var readStats = BenchCore.ComputeStats(readSamples);
StatisticsConsoleRenderer.Dump(readStats);

Console.WriteLine("Benchmark complete.");

await host.StopAsync().ConfigureAwait(false);
