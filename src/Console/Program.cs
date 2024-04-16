using System.ComponentModel;
using System.Diagnostics;
using BenchmarkDotNet.Mathematics;
using Console;
using Contracts;

using Logic;
using Logic.Repositories;
using Logic.Shorteners;
using Spectre.Console;
using Spectre.Console.Cli;

using static BenchmarkCommand;
using Convert = Logic.Convert;

var app = new CommandApp();
app.Configure(config =>
{
    //config.ValidateExamples();

    config.AddCommand<BenchmarkCommand>("bench")
        .WithExample($"bench -s {nameof(ReaderWriterLockSlimUrlRepository)}")
        .WithExample($"bench -s {nameof(ConcurrentUrlRepository)}")
        .WithExample($"bench -s {nameof(PartitionedUrlRepository)}")
        .WithExample($"bench -s {nameof(FASTERUrlRepository)}")
        ;
});

return app.Run(args.Length > 0 ? args : ["bench"]);

[Description("Benchmark the URL shortener")]
internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        internal static class Defaults
        {
            internal const string Url = "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";
            internal const int NumberOfUrls = 10_000_000;
            internal const int Iterations = 3;
            internal static readonly Type RepoType = typeof(FASTERUrlRepository);
            internal static readonly Type ShortenerType = typeof(IncrementingIntegerUrlShortener);
            //internal static readonly Type RepoType = typeof(ReaderWriterLockSlimUrlRepository);
        }

        [CommandOption("-i|--iterations")]
        [DefaultValue(Defaults.Iterations)]
        public int Iterations { get; init; }

        [CommandOption("-n|--urls")]
        [DefaultValue(Defaults.NumberOfUrls)]
        public int NumberOfUrls { get; init; }

        [CommandOption("-u|--url")]
        [DefaultValue(Defaults.Url)]
        public string Url { get; init; }

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

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            SettingsDumper.Dump(settings);

            const int maxDictionaryCapacity = 2_000_000_000;

            var repoType = UrlRepositoryHelper.GetRepositoryType(settings.StorageType);
            var repo = (IUrlRepository)Activator.CreateInstance(repoType);
            var shortener = new IncrementingIntegerUrlShortener(repo);

            var sw = new Stopwatch();
            long workingSet;

            var writeSampleCount = settings.Iterations * settings.NumberOfUrls;
            if (writeSampleCount < 0 || writeSampleCount > maxDictionaryCapacity)
            {
                // We overflowed, set to Int32.MaxValue
                writeSampleCount = maxDictionaryCapacity;
            }

            var readSampleCount = settings.Iterations * settings.NumberOfUrls;
            if (readSampleCount < 0 || readSampleCount > maxDictionaryCapacity)
            {
                // We overflowed, set to Int32.MaxValue
                readSampleCount = maxDictionaryCapacity;
            }

            var readSamples = new List<double>(readSampleCount);
            var writeSamples = new List<double>(writeSampleCount);

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Generating...", ctx =>
                {

                    sw.Start();

                    if (settings.MaxDegreeOfParallelism == 1)
                    {
                        for (var j = 1; j <= settings.Iterations; j++)
                        {
                            sw.Start();
                            for (var i = 0; i < settings.NumberOfUrls; i++)
                            {
                                var shortUrl = shortener.ShortenUrl(settings.Url);

                                if (i % 524288 == 0)
                                {
                                    GC.Collect();
                                    workingSet = Process.GetCurrentProcess().WorkingSet64;

                                    ctx.Status($"Generating... Iteration: {j}, URLs {i}: {workingSet / 1048576}M");
                                }
                            }

                            sw.Stop();
                            writeSamples.Add(settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0));
                            AnsiConsole.MarkupLineInterpolated($"Iteration {j}: Total time to upsert {settings.NumberOfUrls} elements: {sw.ElapsedMilliseconds / 1000.0:0.000} secs ({settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0):0.00} inserts/sec)");
                            sw.Reset();
                        }
                    }
                    else
                    {
                        for (var j = 1; j <= settings.Iterations; j++)
                        {
                            sw.Start();
                            Parallel.For(0, settings.NumberOfUrls,
                                new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism }, i =>
                                {
                                    var shortUrl = shortener.ShortenUrl(settings.Url);

                                    if (i % 524288 == 0)
                                    {
                                        GC.Collect();
                                        workingSet = Process.GetCurrentProcess().WorkingSet64;

                                        ctx.Status($"Generating... Iteration: {j}, URLs {i}: {workingSet / 1048576}M");
                                    }
                                });
                            sw.Stop();
                            writeSamples.Add(settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0));
                            AnsiConsole.MarkupLineInterpolated($"Iteration {j}: Total time to upsert {settings.NumberOfUrls} elements: {sw.ElapsedMilliseconds / 1000.0:0.000} secs ({settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0):0.00} inserts/sec)");
                            sw.Reset();
                        }
                    }

                    workingSet = Process.GetCurrentProcess().WorkingSet64;
                    AnsiConsole.MarkupLineInterpolated($"URLs: {settings.NumberOfUrls * settings.Iterations} Working set: {workingSet / 1048576}M");

                    var writeStatistics = new Statistics(writeSamples);
                    StatisticsDumper.Dump(writeStatistics);
                });

            sw.Reset();

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Uniform random read...", ctx =>
                {
                    // So random is repeatable
                    var rnd = new Random(0);


                    if (settings.MaxDegreeOfParallelism == 1)
                    {
                        for (var j = 1; j <= settings.Iterations; j++)
                        {


                            sw.Start();
                            for (var i = 0; i < settings.NumberOfUrls; i++)
                            {
                                var k = (ulong)rnd.NextInt64(1, settings.NumberOfUrls * j);
                                var shortUrl = Convert.AsBase62String(k).ToString();
                                var longUrl = repo.GetOriginalUrl(shortUrl);

                                if (i % 524288 == 0)
                                {
                                    GC.Collect();
                                    workingSet = Process.GetCurrentProcess().WorkingSet64;

                                    ctx.Status($"Reading... Iteration: {j}, URLs {i}: {workingSet / 1048576}M");
                                }
                            }

                            sw.Stop();
                            readSamples.Add(settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0));
                            AnsiConsole.MarkupLineInterpolated($"Iteration {j}: Total time to read {settings.NumberOfUrls} elements: {sw.ElapsedMilliseconds / 1000.0:0.000} secs ({settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0):0.00} reads/sec)");
                            sw.Reset();
                        }
                    }
                    else
                    {
                        for (var j = 1; j <= settings.Iterations; j++)
                        {
                            sw.Start();
                            Parallel.For(0, settings.NumberOfUrls,
                                new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism }, i =>
                                {
                                    var k = (ulong)rnd.NextInt64(1, settings.NumberOfUrls * j);
                                    var shortUrl = Convert.AsBase62String(k).ToString();
                                    var longUrl = repo.GetOriginalUrl(shortUrl);

                                    if (i % 524288 == 0)
                                    {
                                        GC.Collect();
                                        workingSet = Process.GetCurrentProcess().WorkingSet64;

                                        ctx.Status($"Reading... Iteration: {j}, URLs {i}: {workingSet / 1048576}M");
                                    }
                                });
                            sw.Stop();
                            readSamples.Add(settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0));
                            AnsiConsole.MarkupLineInterpolated($"Iteration {j}: Total time to read {settings.NumberOfUrls} elements: {sw.ElapsedMilliseconds / 1000.0:0.000} secs ({settings.NumberOfUrls / (sw.ElapsedMilliseconds / 1000.0):0.00} reads/sec)");
                            sw.Reset();
                        }
                    }

                    workingSet = Process.GetCurrentProcess().WorkingSet64;
                    AnsiConsole.MarkupLineInterpolated($"URLs: {settings.NumberOfUrls * settings.Iterations} Working set: {workingSet / 1048576}M");

                    var readStatistics = new Statistics(readSamples);
                    StatisticsDumper.Dump(readStatistics);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }

        return 0;
    }
}