using System.Diagnostics;
using System.Security.Principal;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// Allow to pass in arguments from the command line
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, GetGlobalConfig());
return;

static IConfig GetGlobalConfig()
{
    var j = Job.Default
                    .WithGcMode(GcMode.Default.WithServer(true))
                    .AsDefault();

    var config = DefaultConfig.Instance
        .AddJob(j)
        .AddColumn(StatisticColumn.P25)
        .AddColumn(StatisticColumn.P50)
        .AddColumn(StatisticColumn.P95)
        .AddColumn(StatisticColumn.OperationsPerSecond)
        .AddExporter(RPlotExporter.Default, PlainExporter.Default)
        .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(true)))
        .AddDiagnoser(ThreadingDiagnoser.Default)
        ;

    // Allow to disable optimizations when actively debugging
#if DEBUG
    if (Debugger.IsAttached)
    {
        config.WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
#endif

    var windows = false;
    var elevated = false;

#if NET5_0_OR_GREATER
    windows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#endif

    if (windows)
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            elevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        config = config.AddDiagnoser(
            new InliningDiagnoser(true, new[] { "Logic.Shorteners", "Logic.Repositories" }),
            new JitStatsDiagnoser());

        if (elevated)
        {
            config = config.AddDiagnoser(new EtwProfiler());
        }
    }

    return config;
}
