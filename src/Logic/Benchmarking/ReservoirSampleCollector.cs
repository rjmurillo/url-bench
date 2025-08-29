using System.Runtime.CompilerServices;

namespace Logic.Benchmarking;

/// <summary>
/// Fixed-size reservoir sampling collector using Vitter's Algorithm R.
/// Provides an unbiased sample of a potentially unbounded stream.
/// </summary>
public sealed class ReservoirSampleCollector : ISampleCollector
{
    private readonly double[] _buffer;
    private readonly Random _rng;
    private long _seen;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservoirSampleCollector"/> class with the specified capacity and optional random seed.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of samples to retain in the reservoir. Must be greater than zero.
    /// </param>
    /// <param name="seed">
    /// An optional seed value for the random number generator. If not provided, a default seed is used.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is less than or equal to zero.
    /// </exception>
    public ReservoirSampleCollector(int capacity, int? seed = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new double[capacity];
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _seen = 0;
    }

    /// <inheritdoc/>
    public int Count => (int)Math.Min(_seen, _buffer.Length);

    /// <summary>
    /// Adds a numeric sample to the reservoir.
    /// </summary>
    /// <param name="value">The sample value to be added.</param>
    /// <remarks>
    /// If the reservoir is not yet full, the value is added directly. Once the reservoir reaches its capacity,
    /// the value may replace an existing sample with a probability proportional to the number of samples seen so far,
    /// following Vitter's Algorithm R.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(double value)
    {
        long s = _seen;
        _seen = s + 1;
        if (s < _buffer.Length)
        {
            _buffer[s] = value;
            return;
        }

        // Algorithm R: replace a random index with decreasing probability
        int j = _rng.Next((int)_seen); // uniform in [0, seen)
        if (j < _buffer.Length)
        {
            _buffer[j] = value;
        }
    }

    /// <summary>
    /// Retrieves a snapshot of the collected samples.
    /// </summary>
    /// <returns>
    /// A read-only list of doubles representing the current state of the collected samples.
    /// If the internal buffer is partially filled, only the filled portion is returned.
    /// </returns>
    public IReadOnlyList<double> Snapshot()
    {
        // Return just the filled portion
        int count = Count;
        if (count == _buffer.Length)
        {
            return _buffer;
        }

        // copy only when partially filled
        var tmp = new double[count];
        Array.Copy(_buffer, tmp, count);
        return tmp;
    }
}
