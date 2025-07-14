using System;
using System.Diagnostics;

namespace HarpRegulator;

internal sealed class ColoredConsoleTraceListener : ConsoleTraceListener
{
    public ConsoleColor Color { get; init; } = ConsoleColor.DarkGray;

    public ColoredConsoleTraceListener()
        : base()
    { }

    public ColoredConsoleTraceListener(bool useErrorStream)
        : base(useErrorStream)
    { }

    public override void Write(string? message)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = Color;
        base.Write(message);
        Console.ForegroundColor = oldColor;
    }

    public override void WriteLine(string? message)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = Color;
        base.WriteLine(message);
        Console.ForegroundColor = oldColor;
    }
}
