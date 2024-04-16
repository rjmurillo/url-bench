using BenchmarkDotNet.Mathematics;
using Spectre.Console;

namespace Console;

public static class StatisticsDumper
{
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
                displayValue = string.Join(", ", doubleArray.Select(d => d.ToString()));
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
    }
}