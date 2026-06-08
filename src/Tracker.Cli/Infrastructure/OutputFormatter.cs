using Tracker.Core.Enums;
using Tracker.Data.Entities;

namespace Tracker.Cli.Infrastructure;

public static class OutputFormatter
{
    private static readonly Dictionary<MigrationStatus, string> Symbols = new()
    {
        [MigrationStatus.Pending]     = "[ ]",
        [MigrationStatus.InProgress]  = "[~]",
        [MigrationStatus.NeedsReview] = "[?]",
        [MigrationStatus.Done]        = "[x]",
        [MigrationStatus.Skipped]     = "[-]",
        [MigrationStatus.Deferred]    = "[>]"
    };

    private static readonly Dictionary<MigrationStatus, ConsoleColor> Colors = new()
    {
        [MigrationStatus.Pending]     = ConsoleColor.Gray,
        [MigrationStatus.InProgress]  = ConsoleColor.Blue,
        [MigrationStatus.NeedsReview] = ConsoleColor.Yellow,
        [MigrationStatus.Done]        = ConsoleColor.Green,
        [MigrationStatus.Skipped]     = ConsoleColor.DarkGray,
        [MigrationStatus.Deferred]    = ConsoleColor.Magenta
    };

    public static void PrintMethod(Method m)
    {
        var sym = Symbols.GetValueOrDefault(m.Status, "[ ]");
        var col = Colors.GetValueOrDefault(m.Status, ConsoleColor.Gray);

        Console.ForegroundColor = col;
        Console.Write($"{sym} ");
        Console.ResetColor();
        Console.Write($"{m.CurrentName,-45} ");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"{m.ReturnType,-15} ");
        Console.ResetColor();
        Console.WriteLine($"{m.File?.RelativePath}:{m.StartLine}");
    }

    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }
}
