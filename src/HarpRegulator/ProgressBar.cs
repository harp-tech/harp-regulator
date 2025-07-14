using System;
using System.Numerics;

namespace HarpRegulator;

internal struct ProgressBar<T> : IDisposable
    where T : unmanaged, IConvertible, IAdditionOperators<T, T, T>
{
    private readonly bool IsEnabled;
    private string? Units;
    private T Progress;
    private readonly T Maximum;

    public ProgressBar(T maxium, string? units = null, bool isEnabled = true)
    {
        Maximum = maxium;
        Units = units;
        IsEnabled = isEnabled;
        SetProgress(default);
    }

    public void SetProgress(T progress)
    {
        Progress = progress;

        if (!IsEnabled)
            return;

        double percent = Progress.ToDouble(null) / Maximum.ToDouble(null);
        int barWidth = 30;
        int filledWidth = Math.Clamp((int)(percent * (double)barWidth), 0, barWidth);

        // Writing everything in a single Console.Write avoids annoying cursor flickering
        string suffix = Units is null ? $"{percent * 100.0:G1}%" : $"{Progress:N}/{Maximum:N} {Units}";
        Console.Write($"\r  [{new String('=', filledWidth)}{new String(' ', barWidth - filledWidth)}] {suffix} ");
    }

    public void ReportProgress(T chunkSize)
        => SetProgress(Progress + chunkSize);

    public void Dispose()
    {
        if (IsEnabled)
            Console.WriteLine();
    }
}
