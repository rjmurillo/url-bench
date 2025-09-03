using Akka.Actor;
using Akka.Hosting;
using AkkaHost;
using Logic.Contracts;
using Logic.Repositories;
using Logic.Shorteners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UrlConverterPermutations;

public sealed class AkkaUrlManagerAsyncTests : IAsyncLifetime
{
    private IHost? _host;
    private IActorRef? _writer;
    private IActorRef? _reader;

    public async Task InitializeAsync()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUrlShortener, IncrementingIntegerUrlShortener>();
                services.AddSingleton<IUrlRepository, ConcurrentUrlRepository>();

                services.AddAkka("UrlShortenerSystemTest", (builder, sp) =>
                {
                    builder.WithActors((system, registry) =>
                    {
                        var shortener = sp.GetRequiredService<IUrlShortener>();
                        var repo = sp.GetRequiredService<IUrlRepository>();

                        var writer = system.ActorOf(Props.Create(() => new UrlWriteActor(shortener, repo)), "writerTest");
                        var reader = system.ActorOf(Props.Create(() => new UrlReadActor(repo)), "readerTest");
                        registry.Register<UrlWriteActor>(writer);
                        registry.Register<UrlReadActor>(reader);
                    });
                });
            })
            .Build();

        await host.StartAsync().ConfigureAwait(false);

        var registry = host.Services.GetRequiredService<ActorRegistry>();
        _writer = registry.Get<UrlWriteActor>();
        _reader = registry.Get<UrlReadActor>();
        _host = host;
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync().ConfigureAwait(false);
            _host.Dispose();
        }
    }

    [Fact]
    public async Task ShortenAndSaveAsync_Succeeds()
    {
        var mgr = new AkkaUrlManager(_writer!, _reader!, TimeSpan.FromSeconds(5));
        var code = await mgr.ShortenAndSaveAsync("https://example.com/a").ConfigureAwait(false);
        code.ShouldNotBeNull();
        code.Length.ShouldBeGreaterThan(0);

        var original = await mgr.GetOriginalUrlAsync(code).ConfigureAwait(false);
        original.ShouldBe("https://example.com/a");
    }

    private sealed class Blackhole : ReceiveActor { }

    [Fact]
    public async Task ShortenAndSaveAsync_TimesOut()
    {
        var system = ActorSystem.Create("tmp");
        try
        {
            var blackhole = system.ActorOf(Props.Create(() => new Blackhole()));
            var mgr = new AkkaUrlManager(blackhole, _reader!, TimeSpan.FromMilliseconds(50));
            await Should.ThrowAsync<TimeoutException>(async () => await mgr.ShortenAndSaveAsync("https://t").ConfigureAwait(false));
        }
        finally
        {
            await system.Terminate().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task GetOriginalUrlAsync_TimesOut()
    {
        var system = ActorSystem.Create("tmp2");
        try
        {
            var blackhole = system.ActorOf(Props.Create(() => new Blackhole()));
            var mgr = new AkkaUrlManager(_writer!, blackhole, TimeSpan.FromMilliseconds(50));
            await Should.ThrowAsync<TimeoutException>(async () => await mgr.GetOriginalUrlAsync("abc").ConfigureAwait(false));
        }
        finally
        {
            await system.Terminate().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task ShortenAndSaveAsync_Canceled()
    {
        var mgr = new AkkaUrlManager(_writer!, _reader!, TimeSpan.FromSeconds(5));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () => await mgr.ShortenAndSaveAsync("https://x", cts.Token).ConfigureAwait(false));
    }

    [Fact]
    public async Task GetOriginalUrlAsync_Canceled()
    {
        var mgr = new AkkaUrlManager(_writer!, _reader!, TimeSpan.FromSeconds(5));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () => await mgr.GetOriginalUrlAsync("abc", cts.Token).ConfigureAwait(false));
    }
}
