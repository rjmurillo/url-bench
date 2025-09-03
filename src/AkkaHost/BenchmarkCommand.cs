using System.ComponentModel;
using System.Globalization;
using System.Runtime;
using Akka.Actor;
using Akka.Hosting;
using Logic;
using Logic.Benchmarking;
using Logic.Cli;
using Logic.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AkkaHost;

[Description("Benchmark the URL shortener using AkkaHost")]
internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            // Mirror console dumper
            AnsiConsole.Write(new Table().Centered().AddColumns("Name", "Value")
                .AddRow("Iterations", settings.Iterations.ToString(CultureInfo.InvariantCulture))
                .AddRow("NumberOfUrls", settings.NumberOfUrls.ToString(CultureInfo.InvariantCulture))
                .AddRow("Url", settings.Url ?? string.Empty)
                .AddRow("StorageType", settings.StorageType ?? string.Empty)
                .AddRow("ShortenerType", settings.ShortenerType ?? string.Empty)
                .AddRow("MaxDegreeOfParallelism", settings.MaxDegreeOfParallelism.ToString(CultureInfo.InvariantCulture))
                .AddRow("ProgressInterval", settings.ProgressInterval.ToString(CultureInfo.InvariantCulture))
                .AddRow("ReservoirCapacity", settings.ReservoirCapacity.ToString(CultureInfo.InvariantCulture))
                .AddRow("RandomSeed", settings.RandomSeed.ToString(CultureInfo.InvariantCulture))
                .AddRow("MinimalOverhead", settings.MinimalOverhead.ToString(CultureInfo.InvariantCulture))
                .AddRow("UseFastPath", settings.UseFastPath.ToString(CultureInfo.InvariantCulture))
            );

            ThreadPool.GetMinThreads(out var minWorkers, out var minIoThreads);
            ThreadPool.SetMinThreads(Math.Max(minWorkers, Environment.ProcessorCount * 4), minIoThreads);
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            var oldLatency = GCSettings.LatencyMode;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    var repoType = UrlRepositoryHelper.GetRepositoryType(settings.StorageType);
                    var shortenerType = UrlRepositoryHelper.GetShortenerType(settings.ShortenerType);

                    var repo = Factory.CreateRepository(repoType!);
                    var shortener = Factory.CreateShortener(shortenerType!);

                    services.AddSingleton<IUrlShortener>(shortener);
                    services.AddSingleton<IUrlRepository>(repo);

                    services.AddAkka("UrlShortenerSystem", (builder, sp) =>
                    {
                        builder.AddHocon(@"akka {
  log-config-on-start = off
  loglevel = ""ERROR""
  stdout-loglevel = ""ERROR""
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
      throughput = 1000
    }
  }
}", HoconAddMode.Prepend);

                        builder.WithActors((system, registry) =>
                        {
                            var s = sp.GetRequiredService<IUrlShortener>();
                            var r = sp.GetRequiredService<IUrlRepository>();
                            var writer = system.ActorOf(Props.Create(() => new UrlWriteActor(s, r)), "writer");
                            var reader = system.ActorOf(Props.Create(() => new UrlReadActor(r)), "reader");
                            registry.Register<UrlWriteActor>(writer);
                            registry.Register<UrlReadActor>(reader);
                        });
                    });
                })
                .Build();

            host.Start();

            IUrlManager manager = settings.UseFastPath
                ? new AkkaUrlManager(host.Services.GetRequiredService<IUrlShortener>(), host.Services.GetRequiredService<IUrlRepository>())
                : new AkkaUrlManager(host.Services.GetRequiredService<ActorRegistry>().Get<UrlWriteActor>(), host.Services.GetRequiredService<ActorRegistry>().Get<UrlReadActor>());

            BenchmarkRunner.Run(
                manager,
                settings.Iterations,
                settings.NumberOfUrls,
                settings.Url!,
                settings.MaxDegreeOfParallelism,
                settings.ProgressInterval,
                settings.ReservoirCapacity,
                settings.RandomSeed,
                settings.MinimalOverhead);

            host.StopAsync().GetAwaiter().GetResult();
            GCSettings.LatencyMode = oldLatency;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }

        return 0;
    }

    public sealed class Settings : SharedSettings
    {
        [CommandOption("--use-fastpath")]
        [DefaultValue(true)]
        public bool UseFastPath { get; init; }
    }
}
