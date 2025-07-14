using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;

namespace Harp.Devices.Pico;

public sealed class Uf2File
{
    internal readonly string FilePath;
    private readonly byte[] Uf2Bytes;
    public ReadOnlySpan<Uf2Block> Blocks => MemoryMarshal.Cast<byte, Uf2Block>(Uf2Bytes);

    /// <summary>A set of all family IDs within this UF2 marked as being flashable.</summary>
    public ImmutableSortedSet<Uf2FamilyId> FamilyIds { get; }

    public unsafe Uf2File(string filePath)
    {
        FilePath = filePath;
        Uf2Bytes = File.ReadAllBytes(filePath);

        if (Uf2Bytes.Length % sizeof(Uf2Block) != 0)
            throw new InvalidOperationException($"'{filePath}' is not a UF2 or is malformed: The file must be a multiple of {sizeof(Uf2Block)} bytes.");

        ImmutableSortedSet<Uf2FamilyId>.Builder builder = ImmutableSortedSet.CreateBuilder<Uf2FamilyId>();
        int blockIndex = 0;
        foreach (ref readonly Uf2Block block in Blocks)
        {
            if (!block.IsValid)
                throw new InvalidOperationException($"'{filePath}' is not a UF2 or is malformed: Block {blockIndex} is invalid.");
            blockIndex++;

            if (block.Flags.HasFlag(Uf2Flags.NotMainFlash))
                continue;

            // Ignore 0-sized blocks
            // (picotool does this so we do too)
            if (block.PayloadSizeBytes == 0)
                continue;

            if (block.Flags.HasFlag(Uf2Flags.FamilyIdPresent))
                builder.Add((Uf2FamilyId)block.ExtraInfo);
            else
                builder.Add(Uf2FamilyId.None);
        }

        FamilyIds = builder.ToImmutable();
    }

    /// <summary>Flattens this UF2 file into a contiguous array of bytes</summary>
    /// <param name="familyIdFilter">The family ID to select from the file.</param>
    /// <param name="minAddress">The index where data actually starts in the returned buffer</param>
    /// <param name="maxAddress">The exclusive upper bound of the data within the buffer</param>
    /// <returns>A contiguous array of bytes where byte 0 corresponds to address 0.</returns>
    /// <remarks>
    /// Ideally you should avoid using this as it defeats the point of sparse UF2 files, and in some cases using the regions
    /// of 0's added where holes used to be could result data loss in the case that a UF2 was intended explicitly as a patch.
    /// </remarks>
    public byte[] GetFlattened(Uf2FamilyId familyIdFilter, out uint minAddress, out uint maxAddress)
    {
        bool ShouldSkip(in Uf2Block block)
        {
            if (block.Flags.HasFlag(Uf2Flags.NotMainFlash))
                return true;

            if (familyIdFilter != Uf2FamilyId.None)
            {
                if (!block.Flags.HasFlag(Uf2Flags.FamilyIdPresent))
                    return true;
                else if (block.ExtraInfo == (uint)familyIdFilter)
                    return false;
                else
                    return true;
            }
            else
            {
                return block.Flags.HasFlag(Uf2Flags.FamilyIdPresent);
            }
        }

        minAddress = uint.MaxValue;
        maxAddress = 0;
        foreach (ref readonly Uf2Block block in Blocks)
        {
            if (!block.IsValid)
                throw new InvalidOperationException("The UF2 file is malformed.");

            if (ShouldSkip(block))
                continue;

            if (block.TargetAddress < minAddress)
                minAddress = block.TargetAddress;

            uint end = block.TargetAddress + block.PayloadSizeBytes;
            if (end > maxAddress)
                maxAddress = end;
        }

        if (maxAddress == 0 && Blocks.Length > 0)
            throw new InvalidOperationException($"UF2 does not contain flash data{(familyIdFilter == Uf2FamilyId.None ? "" : $" family 0x{familyIdFilter:x4}")}");

        byte[] result = new byte[maxAddress];
        foreach (ref readonly Uf2Block block in Blocks)
        {
            Span<byte> targetSpan = result.AsSpan().Slice(checked((int)block.TargetAddress), checked((int)block.PayloadSizeBytes));
            block.Data.CopyTo(targetSpan);
        }

        return result;
    }

    public unsafe static bool IsUf2File(string filePath)
    {
        using FileStream s = File.OpenRead(filePath);
        Span<Uf2Block> firstBlock = stackalloc Uf2Block[1];
        if (s.Read(MemoryMarshal.AsBytes(firstBlock)) != sizeof(Uf2Block))
            return false;
        return firstBlock[0].IsValid;
    }

    public override string ToString()
        => $"UF2 reader for '{FilePath}'";
}
