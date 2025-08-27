using Akka.Actor;
using Logic;
using Logic.Benchmarking;
using Logic.Contracts;
using Logic.Repositories;
using Logic.Shorteners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using AkkaHost;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using Logic.UI;
using AkkaHost.Messages;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Provide a shortener; repository is not used by the Akka-native aggregate
        services.AddSingleton<IUrlShortener, IncrementingIntegerUrlShortener>();

        services.AddAkka("UrlShortenerSystem", (builder, sp) =>
        {
            builder
                .WithInMemoryJournal()
                .WithInMemorySnapshotStore()
                .WithActors((system, registry) =>
                {
                    var shortener = sp.GetRequiredService<IUrlShortener>();
                    var aggregate = system.ActorOf(Props.Create(() => new UrlShortenerAggregate(shortener)), "aggregate");
                    registry.Register<UrlShortenerAggregate>(aggregate);
                });
        });
    })
    .Build();

await host.StartAsync();

// Bench mode (hard-coded defaults)
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

const string Url = "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";
const long NumberOfUrls = 1_000_000L;
const int Iterations = 1;

var registry = host.Services.GetRequiredService<ActorRegistry>();
var aggregateRef = registry.Get<UrlShortenerAggregate>();
var shortener = host.Services.GetRequiredService<IUrlShortener>();
IUrlManager manager = new AkkaUrlManager(aggregateRef, aggregateRef, shortener);

Console.WriteLine("Starting benchmark (sequential)...");

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
    onIterationCompleted: (j, ips, elapsedMs) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: upserted {NumberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({ips:N0} inserts/sec)"))
);

long workingSetW = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Write complete. Total URLs: {NumberOfUrls * Iterations:N0} Working set: {workingSetW / 1048576}M"));
var writeStats = BenchCore.ComputeStats(writeSamples);
StatisticsConsoleRenderer.Dump(writeStats);

var readSamples = BenchCore.RunReadPhaseSequential(
    Iterations,
    NumberOfUrls,
    manager,
    onProgress: (i, j, rps) =>
    {
        long workingSet = Process.GetCurrentProcess().WorkingSet64;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Progress: read iter {j}, at {i:N0}, {rps:N0} reads/sec, Mem: {workingSet / 1048576}M"));
    },
    onIterationCompleted: (j, rps, elapsedMs) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: read {NumberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({rps:N0} reads/sec)"))
);

long workingSetR = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Read complete. Total URLs: {NumberOfUrls * Iterations:N0} Working set: {workingSetR / 1048576}M"));
var readStats = BenchCore.ComputeStats(readSamples);
StatisticsConsoleRenderer.Dump(readStats);

Console.WriteLine("Benchmark complete.");

await host.StopAsync();