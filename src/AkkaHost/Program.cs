using Logic.Repositories;
using Logic.Shorteners;
using Spectre.Console.Cli;

#pragma warning disable HAA0101
#pragma warning disable SA1600

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<AkkaHost.BenchmarkCommand>("bench")
        .WithExample($"bench -s {nameof(ReaderWriterLockSlimUrlRepository)}")
        .WithExample($"bench -s {nameof(ConcurrentUrlRepository)}")
        .WithExample($"bench -s {nameof(PartitionedUrlRepository)}")
        .WithExample($"bench -s {nameof(FASTERUrlRepository)}")
        .WithExample($"bench -t {nameof(IncrementingIntegerUrlShortener)}")
        .WithExample($"bench -t {nameof(SnowflakeUrlShortener)}")
        .WithExample($"bench -t {nameof(Murmur3HashUrlShortener)}");
});

return app.Run(args.Length > 0 ? args : ["bench"]);
