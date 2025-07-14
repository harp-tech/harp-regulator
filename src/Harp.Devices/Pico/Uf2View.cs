using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Harp.Devices.Pico;

public sealed class Uf2View
{
    private readonly Uf2File File;
    public Uf2FamilyId FamilyId { get; }

    // StartAddress is inclusive, EndAddress is exclusive
    private readonly List<(uint StartAddress, uint EndAddress, int BlockIndex)> BlockMap = new();

    /// <summary>Inclusive lower address targeted by the blocks in this view.</summary>
    public uint MinAddress { get; }
    /// <summary>Exclusive upper address targeted by the blocks in this view.</summary>
    public uint MaxAddress { get; }

    public int BlockCount => BlockMap.Count;

    public Uf2View(Uf2File file, Uf2FamilyId familyIdFilter)
    {
        File = file;
        FamilyId = familyIdFilter;

        MinAddress = uint.MaxValue;
        MaxAddress = 0;

        int blockIndex = -1;
        foreach (ref readonly Uf2Block block in File.Blocks)
        {
            blockIndex++;
            if (block.Flags.HasFlag(Uf2Flags.NotMainFlash))
                continue;

            // Ignore 0-sized blocks
            // (picotool does this so we do too)
            if (block.PayloadSizeBytes == 0)
                continue;

            // Fail if payload is too long
            // (We don't check this until here in case some future UF2 extension uses the field for something else
            if (block.PayloadSizeBytes > Uf2Block.MaxDataSize)
                throw new InvalidOperationException($"UF2 data for family {FamilyId} are malformed: Block {blockIndex} payload size exceeds the maximum.");

            if (block.Flags.HasFlag(Uf2Flags.FamilyIdPresent))
            {
                if (block.ExtraInfo != (uint)familyIdFilter)
                    continue;
            }
            else if (familyIdFilter != Uf2FamilyId.None)
            { continue; }

            AddressRange addressRange = block.AddressRange;

            if (addressRange.Start < MinAddress)
                MinAddress = addressRange.Start;

            if (addressRange.End > MaxAddress)
                MaxAddress = addressRange.End;

            BlockMap.Add((addressRange.Start, addressRange.End, blockIndex));
        }

        BlockMap.Sort((a, b) => a.StartAddress.CompareTo(b.StartAddress));

        if (BlockMap.Count == 0)
            MinAddress = MaxAddress = 0;

        for (int i = 1; i < BlockMap.Count; i++)
        {
            uint previousEnd = BlockMap[i - 1].EndAddress;
            uint currentStart = BlockMap[i].StartAddress;

            if (previousEnd > currentStart)
                throw new InvalidOperationException($"UF2 data for family {FamilyId} are malformed: Block {BlockMap[i - 1].BlockIndex} overlaps with block {BlockMap[i].BlockIndex}.");
        }
    }

    /// <summary>Finds the index of the block mapping entry in <see cref="BlockMap"/> which contains the specified address.</summary>
    /// <returns>
    /// The index of the mapping entry if the address is in the map,
    /// otherwise a negative number that is the bitwise complement of the index of the block following the address.
    /// </returns>
    private int GetBlockMapIndex(uint address)
    {
        int lo = 0;
        int hi = BlockMap.Count - 1;
        while (lo <= hi)
        {
            int i = lo + ((hi - lo) / 2);
            var mapEntry = BlockMap[i];

            if (mapEntry.EndAddress <= address)
            { lo = i + 1; }
            else if (mapEntry.StartAddress > address)
            { hi = i - 1; }
            else
            { return i; }
        }

        return ~lo;
    }

    /// <param name="startAddress">Inclusive start address for the blocks to enumerate</param>
    /// <param name="endAddress">Exclusive end address for the blocks to enumerate</param>
    public BlockEnumerator GetBlocks(uint startAddress, uint endAddress)
    {
        int startIndex = GetBlockMapIndex(startAddress);

        // Start at the block following the start address if that exact address is unavailable
        if (startIndex < 0)
            startIndex = ~startIndex;

        return new BlockEnumerator(this, startIndex, endAddress);
    }

    public BlockEnumerator GetBlocks(AddressRange addressRange)
        => GetBlocks(addressRange.Start, addressRange.End);

    public BlockEnumerator GetEnumerator()
        => new BlockEnumerator(this, 0, uint.MaxValue);

    public struct BlockEnumerator
    {
        private readonly Uf2View View;
        private int BlockMapIndex;
        private int BlockIndex;
        private uint EndAddress;

        public ref readonly Uf2Block Current => ref View.File.Blocks[BlockIndex];

        internal BlockEnumerator(Uf2View view, int startMapIndex, uint endAddress)
        {
            Debug.Assert(startMapIndex >= 0);
            View = view;
            BlockMapIndex = startMapIndex - 1;
            BlockIndex = 0;
            EndAddress = endAddress;
        }

        public bool MoveNext()
        {
            int nextIndex = BlockMapIndex + 1;
            if (nextIndex < View.BlockMap.Count && View.BlockMap[nextIndex].StartAddress < EndAddress)
            {
                BlockMapIndex = nextIndex;
                BlockIndex = View.BlockMap[BlockMapIndex].BlockIndex;
                return true;
            }

            return false;
        }

        public BlockEnumerator GetEnumerator()
            => this;
    }

    public IEnumerable<AddressRange> CoalescedRanges
    {
        get
        {
            AddressRange accumulator = default;
            foreach ((uint startAddress, uint endAddress, _) in BlockMap)
            {
                if (accumulator.End == startAddress)
                { accumulator = new AddressRange(accumulator.Start, endAddress); }
                else
                {
                    if (accumulator.Size != 0)
                        yield return accumulator;

                    accumulator = new AddressRange(startAddress, endAddress);
                }
            }

            if (accumulator.Size != 0)
                yield return accumulator;
        }
    }

    public override string ToString()
        => $"View of '{FamilyId.Description()}' from '{File.FilePath}'";
}
