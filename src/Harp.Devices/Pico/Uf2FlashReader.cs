using PicobootConnection;
using System;

namespace Harp.Devices.Pico;

/// <remarks>This is roughly requivalent to get_iostream_memory_access + iostream_memory_access in picotool.</remarks>
public sealed class Uf2FlashReader : PicoFlashReaderBase
{
    private readonly Uf2View View;
    public override uint BinaryStart { get; }

    public Uf2FlashReader(Uf2View view)
    {
        View = view;
        BinaryStart = PicoMemoryMap.FindBinaryStart(view);
    }

    public override void Read(uint baseAddress, Span<byte> buffer)
        => Read(baseAddress, buffer, fillHolesWithZero: true);

    public void Read(uint baseAddress, Span<byte> buffer, bool fillHolesWithZero)
    {
        uint nextStart = baseAddress;
        foreach (ref readonly Uf2Block block in View.GetBlocks(baseAddress, baseAddress + (uint)buffer.Length))
        {
            // Pad start with zeroes
            if (nextStart < block.TargetAddress)
            {
                if (!fillHolesWithZero)
                    throw new InvalidOperationException($"Region [0x{nextStart:X8}..0x{block.TargetAddress:X8}) does not contain data.");

                int zeroCount = checked((int)(block.TargetAddress - nextStart));
                buffer.Slice(0, zeroCount).Fill(0);
                nextStart = block.TargetAddress;
                buffer = buffer.Slice(zeroCount);
            }

            // Determine the source span within the block
            ReadOnlySpan<byte> data = block.Data;

            uint skipBytes = nextStart - block.TargetAddress;
            if (skipBytes > 0)
                data = data.Slice(checked((int)skipBytes));

            if (data.Length > buffer.Length)
                data = data.Slice(0, buffer.Length);

            // Copy data from block to buffer
            data.CopyTo(buffer);
            nextStart += checked((uint)data.Length);
            buffer = buffer.Slice(data.Length);
        }

        // Pad end with zeroes
        if (buffer.Length > 0)
        {
            if (!fillHolesWithZero)
                throw new InvalidOperationException($"Region [0x{nextStart:X8}..0x{nextStart + (uint)buffer.Length:X8}) does not contain data.");

            buffer.Fill(0);
        }
    }

    public override model_t ReadModel()
        => View.FamilyId.ToPicoModel();

    public override string ToString()
        => $"Virtual flash reader using {View.ToString()}";
}
