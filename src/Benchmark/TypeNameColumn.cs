using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

public class TypeNameColumn : IColumn
{
    public string Id => nameof(TypeNameColumn);
    public string ColumnName { get; } = "Shortener";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Job;
    public int PriorityInCategory => 1;
    public bool IsNumeric => false;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "The type of the URL shortener";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        return GetTypeName(benchmarkCase);
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return GetValue(summary, benchmarkCase);
    }

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    private static string GetTypeName(BenchmarkCase benchmarkCase)
    {
        var type = benchmarkCase.Parameters.Items.FirstOrDefault(item => item.Name == "ShortenerType")?.Value;
        return type == null ? "Unknown" : type.GetType().Name;
    }
}