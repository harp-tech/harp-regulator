using PicobootConnection;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Harp.Devices.Pico;

/// <summary>This extension class contains methods that augment generic UF2 types with Pico-specific functionality</summary>
public static class Uf2PicoExtensions
{
    public static ImmutableSortedSet<memory_type> GetMemoryTypes(this Uf2View view)
    {
        model_t model = view.FamilyId.ToPicoModel();
        if (model == model_t.unknown)
            return ImmutableSortedSet<memory_type>.Empty;

        ImmutableSortedSet<memory_type>.Builder builder = ImmutableSortedSet.CreateBuilder<memory_type>();
        builder.Add(Picoboot.PBC_get_memory_type(view.MinAddress, model));

        // If the start and end of the view have different types, we have a UF2 with multiple types and must scan the whole view to discover all types present
        if (builder.Add(Picoboot.PBC_get_memory_type(view.MaxAddress, model)))
        {
            foreach (ref readonly Uf2Block block in view)
            {
                memory_type startType = Picoboot.PBC_get_memory_type(block.TargetAddress, model);
                builder.Add(startType);

                // picotool makes this assumption, so we assert it as well.
                Debug.Assert(Picoboot.PBC_get_memory_type(block.AddressRange.End, model) == startType, "UF2 blocks are not expected to straddle memory types!");
            }
        }

        return builder.ToImmutable();
    }

    public static AddressRange GetUsedFlashRange(this Uf2View view)
    {
        uint minFlashAddress = uint.MaxValue;
        uint maxFlashAddress = 0;
        model_t model = view.FamilyId.ToPicoModel();
        AddressRange flashRange = PicoMemoryMap.FlashRange(model);
        foreach (ref readonly Uf2Block block in view.GetBlocks(flashRange))
        {
            Debug.Assert(Picoboot.PBC_get_memory_type(block.TargetAddress, model) == memory_type.flash);

            if (block.TargetAddress < minFlashAddress)
                minFlashAddress = block.TargetAddress;

            if (block.EndAddress > maxFlashAddress)
                maxFlashAddress = block.EndAddress;
        }

        if (maxFlashAddress < minFlashAddress)
            return new AddressRange(flashRange.Start, flashRange.Start);

        return new AddressRange(minFlashAddress, maxFlashAddress);
    }

    public static model_t ToPicoModel(this Uf2FamilyId family)
    {
        // This matches behavior of info_command::execute in picotool.
        switch (family)
        {
            case Uf2FamilyId.RP2040:
                return model_t.rp2040;
            case Uf2FamilyId.RP2350_ARM_S:
            case Uf2FamilyId.RP2350_RISCV:
            case Uf2FamilyId.RP2350_ARM_NS:
                return model_t.rp2350;
            default:
                return model_t.unknown;
        }
    }

    public static bool IsPico(this Uf2FamilyId family)
        => family.ToPicoModel() != model_t.unknown;
}
