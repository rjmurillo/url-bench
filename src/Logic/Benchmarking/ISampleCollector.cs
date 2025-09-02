namespace Logic.Benchmarking;

/// <summary>
/// Collects numeric samples in a memory-bounded way suitable for very large runs.
/// </summary>
public interface ISampleCollector
{
    /// <summary>Adds a new sample to the collector.</summary>
    /// <param name="value">The numeric value to add.</param>
    void Add(double value);

    /// <summary>
    /// Returns a stable snapshot of the current collected samples.
    /// Implementations should avoid copying where possible.
    /// </summary>
    /// <returns>A read-only view of samples.</returns>
    IReadOnlyList<double> Snapshot();

    /// <summary>Gets number of samples currently retained.</summary>
    long Count { get; }
}
