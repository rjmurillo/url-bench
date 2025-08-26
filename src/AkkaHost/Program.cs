using Akka.Actor;
using Logic.Contracts;
using Logic.Repositories;
using Logic.Shorteners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using AkkaHost;


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

Console.WriteLine("Akka.NET started via Akka.Hosting (no HOCON).");
Console.WriteLine("Press Ctrl+C to exit.");

await host.WaitForShutdownAsync();