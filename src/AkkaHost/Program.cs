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

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register your IUrlRepository / IUrlShortener implementations here if not already registered elsewhere
        services.AddSingleton<IUrlRepository, ConcurrentUrlRepository>();
        services.AddSingleton<IUrlShortener, IncrementingIntegerUrlShortener>();

        services.AddAkka("UrlShortenerSystem", (builder, sp) =>
        {
            // No HOCON: configure Akka.Persistence via Akka.Persistence.Hosting extensions
            builder
                .WithInMemoryJournal()
                .WithInMemorySnapshotStore()
                .WithActors((system, registry) =>
                {
                    var repository = sp.GetRequiredService<IUrlRepository>();

                    var projection = system.ActorOf(Props.Create(() => new ProjectionActor(repository)), "projection");
                    registry.Register<ProjectionActor>(projection);

                    var query = system.ActorOf(Props.Create(() => new QueryActor(repository)), "query");
                    registry.Register<QueryActor>(query);

                    var shortener = sp.GetRequiredService<IUrlShortener>();
                    var aggregate = system.ActorOf(Props.Create(() => new UrlShortenerAggregate(projection, shortener, repository)), "aggregate");
                    registry.Register<UrlShortenerAggregate>(aggregate);
                });
        });
    })
    .Build();

await host.StartAsync();

// Bench mode (hard-coded defaults)
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

const string Url = "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";
const long NumberOfUrls = 10_000_000L;
const int Iterations = 3;

var repo = host.Services.GetRequiredService<IUrlRepository>();
var shortener = host.Services.GetRequiredService<IUrlShortener>();
IUrlManager manager = new UrlManager(shortener, repo);

Console.WriteLine("Starting benchmark (sequential)...");

var writeSamples = BenchCore.RunWritePhaseSequential(
    Iterations,
    NumberOfUrls,
    Url,
    manager,
    onProgress: (i, j, ips) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Progress: write iter {j}, at {i:N0}, {ips:N0} inserts/sec")),
    onIterationCompleted: (j, ips, elapsedMs) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: upserted {NumberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({ips:N0} inserts/sec)"))
);

long workingSetW = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Write complete. Total URLs: {NumberOfUrls * Iterations:N0} Working set: {workingSetW / 1048576}M"));
var writeStats = BenchCore.ComputeStats(writeSamples);
StatisticsDumper.Dump(writeStats);

var readSamples = BenchCore.RunReadPhaseSequential(
    Iterations,
    NumberOfUrls,
    manager,
    onProgress: (i, j, rps) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Progress: read iter {j}, at {i:N0}, {rps:N0} reads/sec")),
    onIterationCompleted: (j, rps, elapsedMs) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Iteration {j}: read {NumberOfUrls:N0} in {elapsedMs / 1000.0:N3}s ({rps:N0} reads/sec)"))
);

long workingSetR = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Read complete. Total URLs: {NumberOfUrls * Iterations:N0} Working set: {workingSetR / 1048576}M"));
var readStats = BenchCore.ComputeStats(readSamples);
StatisticsDumper.Dump(readStats);

Console.WriteLine("Benchmark complete.");

await host.StopAsync();