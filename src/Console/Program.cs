using Console;
using Logic.Repositories;
using Logic.Shorteners;
using Spectre.Console.Cli;

#pragma warning disable HAA0101
#pragma warning disable SA1600

// Bump ThreadPool minimum threads to reduce ramp-up stalls under load
ThreadPool.GetMinThreads(out var minWorkers, out var minIoThreads);
var targetWorkers = Math.Max(minWorkers, Environment.ProcessorCount * 4);
ThreadPool.SetMinThreads(targetWorkers, minIoThreads);

var app = new CommandApp();
app.Configure(config =>
{
    // config.ValidateExamples();
    config.AddCommand<BenchmarkCommand>("bench")
        .WithExample($"bench -s {nameof(ReaderWriterLockSlimUrlRepository)}")
        .WithExample($"bench -s {nameof(ConcurrentUrlRepository)}")
        .WithExample($"bench -s {nameof(PartitionedUrlRepository)}")
        .WithExample($"bench -s {nameof(FASTERUrlRepository)}")
        .WithExample($"bench -t {nameof(IncrementingIntegerUrlShortener)}")
        .WithExample($"bench -t {nameof(SnowflakeUrlShortener)}")
        .WithExample($"bench -t {nameof(Murmur3HashUrlShortener)}")
        ;
});

return app.Run(args.Length > 0 ? args : ["bench"]);
