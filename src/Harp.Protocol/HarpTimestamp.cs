using System.Runtime.InteropServices;

namespace Harp.Protocol;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HarpTimestamp
{
    public readonly uint RawSeconds;
    public readonly ushort RawMicroseconds;

    public HarpTimestamp(uint rawSeconds, ushort rawMicroseconds)
    {
        RawSeconds = rawSeconds;
        RawMicroseconds = rawMicroseconds;
    }

    public double Seconds => (double)RawSeconds + (double)(RawMicroseconds * 32) * 1e-6;

    public static HarpTimestamp Invalid => new(uint.MaxValue, ushort.MaxValue);

    public override string ToString()
        => $"{Seconds}s";
}
