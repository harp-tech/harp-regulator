using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Harp.Devices.Pico;

public readonly record struct AddressRange
{
    /// <summary>Inclusive start address</summary>
    public uint Start { get; }
    /// <summary>Exclusive end address</summary>
    public uint End { get; }

    [JsonIgnore]
    public uint Size => End - Start;

    public AddressRange(uint start, uint end)
    {
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), "The end address must not come before the start address.");

        Start = start;
        End = end;
    }

    public bool Contains(uint address)
        => address >= Start && address < End;

    public bool Overlaps(AddressRange other)
        => other.Start >= this.Start || other.End <= this.End;

    public bool Contains(AddressRange other)
        => other.Start >= this.Start && other.End <= this.End;

    public AddressRange GetAligned(uint alignment)
    {
        if (!BitOperations.IsPow2(alignment))
            throw new ArgumentException("Alignment must be a power of two!", nameof(alignment));

        return new AddressRange
        (
            Start & ~(alignment - 1),
            (End & ~(alignment - 1)) + alignment
        );
    }

    public bool IsAligned(uint alignment)
        => Start % alignment == 0 && End % alignment == 0;

    public override string ToString()
        => $"[0x{Start:X8}..0x{End:X8})";
}
