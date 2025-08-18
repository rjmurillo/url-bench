using Spectre.Console;
using Spectre.Console.Cli;

namespace Console;

/// <summary>
/// Provides utility methods for displaying command settings in a formatted table.
/// </summary>
internal static class SettingsDumper
{
    /// <summary>
    /// Displays the properties and values of the specified command settings in a formatted table.
    /// </summary>
    /// <param name="settings">The command settings to display.</param>
    public static void Dump(CommandSettings settings)
    {
        var table = new Table().RoundedBorder();
        table.AddColumn("[grey]Name[/]");
        table.AddColumn("[grey]Value[/]");

        var properties = settings.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(settings)
                ?.ToString()
                ?.Replace("[", "[[");

#pragma warning disable HAA0101 // Array allocation for params parameter
            table.AddRow(
                property.Name,
                value ?? "[grey]null[/]");
#pragma warning restore HAA0101 // Array allocation for params parameter
        }

        AnsiConsole.Write(table);
    }
}
