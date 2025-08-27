using System.Globalization;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using Spectre.Console;

namespace Logic.UI;

/// <summary>
/// Provides utility methods for displaying benchmark statistics in a formatted manner.
/// </summary>
public static class StatisticsConsoleRenderer
{
    /// <summary>
    /// Displays the benchmark statistics in a formatted table and handles outlier detection.
    /// </summary>
    /// <param name="statistics">The statistics to display.</param>
    public static void Dump(Statistics statistics)
    {
        var table = new Table().RoundedBorder();
        table.AddColumn("[grey]Name[/]");
        table.AddColumn("[grey]Value[/]");

        var properties = statistics.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(statistics);
            string? displayValue = null;
            if (value is double[] doubleArray)
            {
                displayValue = string.Join(", ", doubleArray.Select(d => d.ToString("N0")));
            }
            else if (value is double doubleValue)
            {
                displayValue = doubleValue.ToString("N0");
            }
            else
            {
                // Convert other values to string and escape brackets
                displayValue = value?.ToString()
                    ?.Replace("[", "[[")
                    ?.Replace("]", "]]");
            }

            table.AddRow(
                property.Name,
                displayValue ?? "[grey]null[/]");
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine(statistics.ToString(
            CultureInfo.CurrentUICulture,
            x => x.ToString("N0"),
            true));

        var allOutliers = statistics.AllOutliers;

        // TODO: Improve
        var actualOutliers = statistics.GetActualOutliers(OutlierMode.RemoveAll);

        if (allOutliers.Any())
        {
            AnsiConsole.WriteLine(GetMessage(actualOutliers, allOutliers, statistics.LowerOutliers, statistics.UpperOutliers, CultureInfo.CurrentUICulture));

            statistics = new Statistics(statistics.OriginalValues.Except(allOutliers));
            Dump(statistics);
        }
    }

    /// <summary>
    /// Generates a message describing the outlier detection and removal process.
    /// </summary>
    /// <param name="actualOutliers">The actual outliers that were removed.</param>
    /// <param name="allOutliers">All detected outliers.</param>
    /// <param name="lowerOutliers">Lower outliers.</param>
    /// <param name="upperOutliers">Upper outliers.</param>
    /// <param name="cultureInfo">The culture information for formatting.</param>
    /// <returns>A formatted message describing the outlier processing.</returns>
    public static string GetMessage(double[] actualOutliers, double[] allOutliers, double[] lowerOutliers, double[] upperOutliers, CultureInfo cultureInfo)
    {
        if (allOutliers.Length == 0)
        {
            return string.Empty;
        }

        string Format(int n, string verb)
        {
            string words = n == 1 ? "outlier  was " : "outliers were";
            return $"{n} {words} {verb}";
        }

        var rangeMessages = new List<string?> { GetRangeMessage(lowerOutliers), GetRangeMessage(upperOutliers) };
        rangeMessages.RemoveAll(string.IsNullOrEmpty);
        string rangeMessage = rangeMessages.Any()
            ? " (" + string.Join(", ", rangeMessages) + ")"
            : string.Empty;

        if (actualOutliers.Length == allOutliers.Length)
        {
            return Format(actualOutliers.Length, "removed") + rangeMessage;
        }

        if (actualOutliers.Length == 0)
        {
            return Format(allOutliers.Length, "detected") + rangeMessage;
        }

        return Format(actualOutliers.Length, "removed") + ", " + Format(allOutliers.Length, "detected") + rangeMessage;
    }

    private static string? GetRangeMessage(double[] values)
    {
        string Format(double value) => TimeInterval.FromSeconds(value).ToString();

        return values.Length switch
        {
            0 => null,
            1 => Format(values.First()),
            2 => Format(values.Min()) + ", " + Format(values.Max()),
            _ => Format(values.Min()) + ".." + Format(values.Max()),
        };
    }
}
