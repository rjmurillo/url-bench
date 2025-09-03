using Logic.Benchmarking;

namespace UrlConverterPermutations;

public class ReservoirSampleCollectorTests
{
    [Fact]
    public void PartialFill_SnapshotCountEqualsAddedAndCollectorCount()
    {
        // Arrange
        const int capacity = 10;
        const int added = 7;
        var collector = new ReservoirSampleCollector(capacity, seed: 123);

        // Act
        for (int i = 1; i <= added; i++)
        {
            collector.Add(i);
        }

        var snapshot = collector.Snapshot();

        // Assert
        Assert.Equal(added, collector.Count);
        Assert.Equal(collector.Count, snapshot.Count);
        Assert.Equal(Enumerable.Range(1, added).Select(i => (double)i), snapshot);
    }

    [Fact]
    public void FullSnapshot_IsImmutableAfterFurtherAdds()
    {
        // Arrange
        const int capacity = 5;
        var collector = new ReservoirSampleCollector(capacity, seed: 42);

        // Fill to capacity with deterministic values
        for (int i = 0; i < capacity; i++)
        {
            collector.Add(i);
        }

        // Act
        var snap = collector.Snapshot();
        var snapCopy = snap.ToArray(); // Expected immutable contents

        // Perform additional adds that would go through replacement path
        for (int i = 100; i < 120; i++)
        {
            collector.Add(i);
        }

        // Assert
        Assert.Equal(capacity, snap.Count); // length must never change
        Assert.Equal(snapCopy.Length, snap.Count);
        Assert.Equal(snapCopy, snap); // contents should not change for a previously returned snapshot
    }

    [Fact]
    public void ReplacementPath_SmokeCheck_CountEqualsCapacityAndValuesInRange()
    {
        // Arrange
        const int capacity = 50;
        const int totalAdds = 5_000;
        var collector = new ReservoirSampleCollector(capacity, seed: 12345);

        // Act
        for (int i = 0; i < totalAdds; i++)
        {
            collector.Add(i);
        }

        var snapshot = collector.Snapshot();

        // Assert
        Assert.Equal(capacity, collector.Count);
        Assert.Equal(capacity, snapshot.Count);

        // All values should be from the input range
        Assert.All(snapshot, v => Assert.InRange(v, 0.0, totalAdds - 1));

        // No obvious bias: expect coverage across the range (at least one from early and late segments)
        Assert.Contains(snapshot, v => v < totalAdds * 0.25);
        Assert.Contains(snapshot, v => v > totalAdds * 0.75);
    }

    [Fact]
    public void Count_Semantics_IncreaseUntilCapacityThenStays()
    {
        // Arrange
        const int capacity = 10;
        const int totalAdds = 30;
        var collector = new ReservoirSampleCollector(capacity, seed: 7);

        // Act & Assert
        for (int i = 0; i < totalAdds; i++)
        {
            collector.Add(i);
            var expected = Math.Min(i + 1, capacity);
            Assert.Equal(expected, collector.Count);
        }

        // After all adds, Count must equal capacity
        Assert.Equal(capacity, collector.Count);
    }
}
