using Benchmark;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

public abstract class ShortenerColumnBase : IColumn
{
    public abstract string Id { get; }
    public abstract string ColumnName { get; }
    public virtual bool AlwaysShow => true;
    public virtual ColumnCategory Category => ColumnCategory.Job;
    public virtual int PriorityInCategory => 1;
    public virtual bool IsNumeric => false;
    public virtual UnitType UnitType => UnitType.Dimensionless;
    public abstract string Legend { get; }

    public virtual string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        return GetTypeName(benchmarkCase);
    }

    public virtual string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return GetValue(summary, benchmarkCase);
    }

    public virtual bool IsAvailable(Summary summary) => true;

    public virtual bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    protected virtual string GetTypeName(BenchmarkCase benchmarkCase) => "Unknown";
}

public class ShortenerTypeColumn : ShortenerColumnBase
{
    public override string Id => nameof(ShortenerTypeColumn);
    public override string ColumnName { get; } = "Shortener";
    public override string Legend => "The type of the URL shortener";

    protected override string GetTypeName(BenchmarkCase benchmarkCase)
    {
        var type = benchmarkCase.Parameters.Items.FirstOrDefault(item => item.Name == nameof(IShortenerBenchmark.ShortenerType))?.Value;

        if (type is Type t)
        {
            return t.Name;
        }

        return base.GetTypeName(benchmarkCase);
    }
}

public class RepositoryTypeColumn : ShortenerColumnBase
{
    public override string Id => nameof(RepositoryTypeColumn);
    public override string ColumnName { get; } = "Repository";
    public override string Legend => "The type of the URL repository";

    protected override string GetTypeName(BenchmarkCase benchmarkCase)
    {
        var type = benchmarkCase.Parameters.Items.FirstOrDefault(item => item.Name == nameof(IRepositoryBenchmark.RepositoryType))?.Value;

        if (type is Type t)
        {
            return t.Name;
        }

        return base.GetTypeName(benchmarkCase);
    }
}