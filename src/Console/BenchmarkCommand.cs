using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Mathematics;
using Contracts;
using Logic;
using Logic.Contracts;
using Logic.Repositories;
using Logic.Shorteners;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable SA1600
#pragma warning disable HAA0301

namespace Console;

[Description("Benchmark the URL shortener")]
internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    private const int ProgressInterval = 524_288;

#pragma warning disable MA0051
    public override int Execute(CommandContext context, Settings settings)
#pragma warning restore MA0051
    {
        try
        {
            SettingsDumper.Dump(settings);

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

            const int maxDictionaryCapacity = 2_000_000_000;

            var repoType = UrlRepositoryHelper.GetRepositoryType(settings.StorageType);
            var repo = Factory.CreateRepository(repoType!);
            var shortenerType = UrlRepositoryHelper.GetShortenerType(settings.ShortenerType);
            var shortener = Factory.CreateShortener(shortenerType!);
            var facade = new UrlManager(shortener, repo);
            var sw = new Stopwatch();
            string url = settings.Url!;

            long writeSampleCount = settings.Iterations * settings.NumberOfUrls;
            if (writeSampleCount < 0 || writeSampleCount > maxDictionaryCapacity)
            {
                writeSampleCount = maxDictionaryCapacity;
            }

            long readSampleCount = settings.Iterations * settings.NumberOfUrls;
            if (readSampleCount < 0 || readSampleCount > maxDictionaryCapacity)
            {
                readSampleCount = maxDictionaryCapacity;
            }

            var readSamples = new List<double>((int)Math.Min(readSampleCount, int.MaxValue));
            var writeSamples = new List<double>((int)Math.Min(writeSampleCount, int.MaxValue));

            RunWritePhase(settings, facade, url, sw, writeSamples);
            RunReadPhase(settings, facade, sw, readSamples);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }

        return 0;
    }

    private static void RunWritePhase(Settings settings, IUrlManager facade, string url, Stopwatch sw, List<double> samples)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Generating...", ctx =>
            {
                if (settings.MaxDegreeOfParallelism == 1)
                {
                    for (int j = 1; j <= settings.Iterations; j++)
                    {
                        sw.Restart();
                        long lastMs = 0;
                        for (long i = 0; i < settings.NumberOfUrls; i++)
                        {
                            facade.ShortenAndSave(url);

                            if (i != 0 && (i % ProgressInterval) == 0)
                            {
                                long elapsedMs = sw.ElapsedMilliseconds - lastMs;
                                lastMs = sw.ElapsedMilliseconds;
                                if (elapsedMs > 0)
                                {
                                    samples.Add(ProgressInterval / (elapsedMs / 1000.0));
                                }

                                long workingSet = Process.GetCurrentProcess().WorkingSet64;
                                ctx.Status(string.Create(CultureInfo.InvariantCulture, $"Generating... Iteration: {j}, URLs {i:N0}: {workingSet / 1048576}M"));
                            }
                        }

                        sw.Stop();
                        if (sw.ElapsedMilliseconds > 0)
                        {
                            samples.Add(settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0));
                        }

                        AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"Iteration {j}: Total time to upsert {settings.NumberOfUrls:N0} elements: {sw.ElapsedMilliseconds / 1000.0:N3} secs ({(sw.ElapsedMilliseconds > 0 ? settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0) : double.NaN):N0} inserts/sec)");
                    }
                }
                else
                {
                    for (int j = 1; j <= settings.Iterations; j++)
                    {
                        var swLocal = Stopwatch.StartNew();
                        var options = new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism };
                        Parallel.For(0L, settings.NumberOfUrls, options, _ => facade.ShortenAndSave(url));
                        swLocal.Stop();
                        if (swLocal.ElapsedMilliseconds > 0)
                        {
                            samples.Add(settings.NumberOfUrls / (swLocal.ElapsedMilliseconds / 1000.0));
                        }

                        AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"Iteration {j}: Total time to upsert {settings.NumberOfUrls:N0} elements: {swLocal.ElapsedMilliseconds / 1000.0:N3} secs ({(swLocal.ElapsedMilliseconds > 0 ? settings.NumberOfUrls / (swLocal.ElapsedMilliseconds / 1000.0) : double.NaN):N0} inserts/sec)");
                    }
                }

                long workingSet2 = Process.GetCurrentProcess().WorkingSet64;
                AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"URLs: {settings.NumberOfUrls * settings.Iterations} Working set: {workingSet2 / 1048576}M");

                var writeStats = new Statistics(samples.Where(p => !double.IsNaN(p) && !double.IsInfinity(p)));
                StatisticsDumper.Dump(writeStats);
            });
    }

    private static void RunReadPhase(Settings settings, IUrlManager facade, Stopwatch sw, List<double> samples)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Uniform random read...", ctx =>
            {
                var rnd = new Random(0); // Initialize random number generator

                if (settings.MaxDegreeOfParallelism == 1)
                {
                    for (int j = 1; j <= settings.Iterations; j++)
                    {
                        sw.Restart();
                        long lastMs = 0;
                        for (long i = 0; i < settings.NumberOfUrls; i++)
                        {
                            ulong k = (ulong)rnd.NextInt64(1, settings.NumberOfUrls * j);
                            ReadSingle(facade, k);

                            if (i != 0 && (i % ProgressInterval) == 0)
                            {
                                long elapsedMs = sw.ElapsedMilliseconds - lastMs;
                                lastMs = sw.ElapsedMilliseconds;
                                if (elapsedMs > 0)
                                {
                                    samples.Add(ProgressInterval / (elapsedMs / 1000.0));
                                }

                                long workingSet = Process.GetCurrentProcess().WorkingSet64;
                                ctx.Status(string.Create(CultureInfo.InvariantCulture, $"Reading... Iteration: {j}, URLs {i:N0}: {workingSet / 1048576}M"));
                            }
                        }

                        sw.Stop();
                        if (sw.ElapsedMilliseconds > 0)
                        {
                            samples.Add(settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0));
                        }

                        AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"Iteration {j}: Total time to read {settings.NumberOfUrls:N0} elements: {sw.ElapsedMilliseconds / 1000.0:N3} secs ({(sw.ElapsedMilliseconds > 0 ? settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0) : double.NaN):N0} reads/sec)");
                    }
                }
                else
                {
                    Parallel.For(1, settings.Iterations + 1, new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism }, j =>
                    {
                        var localRnd = new Random(unchecked(397 ^ j));
                        var swLocal = Stopwatch.StartNew();
                        for (long i = 0; i < settings.NumberOfUrls; i++)
                        {
                            ulong k = (ulong)localRnd.NextInt64(1, settings.NumberOfUrls * j);
                            ReadSingle(facade, k);
                        }

                        swLocal.Stop();
                        if (swLocal.ElapsedMilliseconds > 0)
                        {
                            lock (samples)
                            {
                                samples.Add(settings.NumberOfUrls / (swLocal.ElapsedMilliseconds / 1000.0));
                            }
                        }

                        AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"Iteration {j}: Total time to read {settings.NumberOfUrls:N0} elements: {swLocal.ElapsedMilliseconds / 1000.0:N3} secs ({(swLocal.ElapsedMilliseconds > 0 ? settings.NumberOfUrls / (swLocal.ElapsedMilliseconds / 1000.0) : double.NaN):N0} reads/sec)");
                    });
                }

                long workingSet2 = Process.GetCurrentProcess().WorkingSet64;
                AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"URLs: {settings.NumberOfUrls * settings.Iterations} Working set: {workingSet2 / 1048576}M");

                var readStats = new Statistics(samples.Where(p => !double.IsNaN(p) && !double.IsInfinity(p)));
                StatisticsDumper.Dump(readStats);
            });
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

    public sealed class Settings : CommandSettings
    {
        internal static class Defaults
        {
            [SuppressMessage("Security", "S1075:URIs should not be hardcoded", Justification = "Benchmark default only; no network calls")]
            internal const string Url = "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";
            internal const long NumberOfUrls = 10_000_000L;
            internal const int Iterations = 3;
            internal static readonly Type RepoType = typeof(FASTERUrlRepository);
            internal static readonly Type ShortenerType = typeof(IncrementingIntegerUrlShortener);
        }

        [CommandOption("-i|--iterations")]
        [DefaultValue(Defaults.Iterations)]
        public int Iterations { get; init; }

        [CommandOption("-n|--urls")]
        [DefaultValue(Defaults.NumberOfUrls)]
        public long NumberOfUrls { get; init; }

        [CommandOption("-u|--url")]
        [DefaultValue(Defaults.Url)]
        public string? Url { get; init; }

        [CommandOption("-s|--storage")]
        public string? StorageType { get; private set; }

        [CommandOption("-t|--urlshortener")]
        public string? ShortenerType { get; private set; }

        [CommandOption("-p|--maxdop")]
        [DefaultValue(1)]
        public int MaxDegreeOfParallelism { get; init; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(StorageType))
            {
                StorageType = Defaults.RepoType.Name;
            }

            if (string.IsNullOrWhiteSpace(ShortenerType))
            {
                ShortenerType = Defaults.ShortenerType.Name;
            }

            var typesImplementingIUrlRepository = UrlRepositoryHelper.GetConcreteTypes<IUrlRepository>();

            var storageValidationResult = UrlRepositoryHelper.GetRepositoryType(StorageType) != null
                ? ValidationResult.Success()
                : ValidationResult.Error(
                    $"Unrecognized storage type '{StorageType}'. Use: {string.Join(", ", typesImplementingIUrlRepository.Select(t => t.Name))}");

            if (!storageValidationResult.Successful)
            {
                return storageValidationResult;
            }

            var typesImplementingIUrlShortener = UrlRepositoryHelper.GetConcreteTypes<IUrlShortener>();

            var shortenerValidationResult = UrlRepositoryHelper.GetShortenerType(ShortenerType) != null
                ? ValidationResult.Success()
                : ValidationResult.Error(
                    $"Unrecognized URL shortener type '{ShortenerType}'. Use: {string.Join(", ", typesImplementingIUrlShortener.Select(t => t.Name))}");

            return shortenerValidationResult;
        }
    }
}
