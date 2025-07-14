using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Harp.Devices.Pico;

public unsafe readonly struct Uf2Block
{
    public readonly uint MagicStart0;
    public readonly uint MagicStart1;
    public readonly Uf2Flags Flags;
    public readonly uint TargetAddress;
    public readonly uint PayloadSizeBytes;
    public readonly uint BlockNumber;
    public readonly uint BlockCount;
    public readonly uint ExtraInfo; // File size or board family ID or zero. Family ID is considered best practice for modern boards. See flags.

    public const int MaxDataSize = 476;
    private struct __Data
    {
        public fixed byte Data[MaxDataSize];
    }
    private readonly __Data _Data;
    public ReadOnlySpan<byte> Data => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _Data.Data[0]), Math.Min(MaxDataSize, checked((int)PayloadSizeBytes)));

    public readonly uint MagicEnd;

    public const uint ExpectedMagicStart0 = 0x0A324655;
    public const uint ExpectedMagicStart1 = 0x9E5D5157;
    public const uint ExpectedMagicEnd = 0x0AB16F30;

    public bool IsValid
        => MagicStart0 == ExpectedMagicStart0
        && MagicStart1 == ExpectedMagicStart1
        && MagicEnd == ExpectedMagicEnd;

    public uint EndAddress => TargetAddress + PayloadSizeBytes;
    public AddressRange AddressRange => new(TargetAddress, EndAddress);
}
