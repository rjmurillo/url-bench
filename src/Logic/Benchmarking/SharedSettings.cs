using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Logic.Benchmarking;
using Logic.Repositories;
using Logic.Shorteners;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Logic.Cli;

/// <summary>
/// Shared CLI settings to be used by Console and AkkaHost to avoid duplication.
/// </summary>
public class SharedSettings : CommandSettings
{
    internal static class Defaults
    {
        [SuppressMessage("Security", "S1075:URIs should not be hardcoded", Justification = "Benchmark default only; no network calls")]
        internal const string Url = "https://www.microsoft.com/en-us/security/business/endpoint-security/microsoft-defender-endpoint";
        internal const long NumberOfUrls = 10_000_000L;
        internal const int Iterations = 3;
        internal static readonly Type RepoType = typeof(FASTERUrlRepository);
        internal static readonly Type ShortenerType = typeof(IncrementingIntegerUrlShortener);
        internal const int ProgressInterval = BenchCore.ProgressInterval;
        internal const int ReservoirCapacity = 8192;
        internal const int RandomSeed = 0;
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
    public string? StorageType { get; set; }

    [CommandOption("-t|--urlshortener")]
    public string? ShortenerType { get; set; }

    [CommandOption("-p|--maxdop")]
    [DefaultValue(1)]
    public int MaxDegreeOfParallelism { get; init; }

    [CommandOption("--progress-interval")]
    [DefaultValue(Defaults.ProgressInterval)]
    public int ProgressInterval { get; init; }

    [CommandOption("--reservoir")]
    [DefaultValue(Defaults.ReservoirCapacity)]
    public int ReservoirCapacity { get; init; }

    [CommandOption("--seed")]
    [DefaultValue(Defaults.RandomSeed)]
    public int RandomSeed { get; init; }

    // Common flag supported by both hosts
    [CommandOption("--minimal-overhead")]
    [DefaultValue(false)]
    public bool MinimalOverhead { get; init; }

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

        if (ProgressInterval <= 0)
        {
            return ValidationResult.Error("--progress-interval must be > 0");
        }

        if (ReservoirCapacity <= 0)
        {
            return ValidationResult.Error("--reservoir must be > 0");
        }

        return ValidationResult.Success();
    }
}
