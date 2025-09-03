using System.ComponentModel;
using System.Runtime;
using Logic;
using Logic.Benchmarking;
using Logic.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable SA1600
#pragma warning disable HAA0301

namespace Console;

[Description("Benchmark the URL shortener")]
internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
#pragma warning disable MA0051
    public override int Execute(CommandContext context, Settings settings)
#pragma warning restore MA0051
    {
        try
        {
            SettingsDumper.Dump(settings);

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            var oldLatency = GCSettings.LatencyMode;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            var repoType = UrlRepositoryHelper.GetRepositoryType(settings.StorageType);
            var repo = Factory.CreateRepository(repoType!);
            var shortenerType = UrlRepositoryHelper.GetShortenerType(settings.ShortenerType);
            var shortener = Factory.CreateShortener(shortenerType!);
            var facade = new UrlManager(shortener, repo);
            string url = settings.Url!;

            BenchmarkRunner.Run(
                facade,
                settings.Iterations,
                settings.NumberOfUrls,
                url,
                settings.MaxDegreeOfParallelism,
                settings.ProgressInterval,
                settings.ReservoirCapacity,
                settings.RandomSeed,
                settings.MinimalOverhead);

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
    }
}
