using PicobootConnection;

namespace Harp.Devices.Pico;

internal static class PicoMemoryMap
{
    public const uint ROM_START = 0x00000000;
    public const uint ROM_END_RP2040 = 0x00004000;
    public const uint ROM_END_RP2350 = 0x00008000;

    public const uint FLASH_START = 0x10000000;
    public const uint FLASH_END_RP2040 = 0x11000000;
    public const uint FLASH_END_RP2350 = 0x12000000;

    public const uint XIP_SRAM_START_RP2040 = 0x15000000;
    public const uint XIP_SRAM_END_RP2040 = 0x15004000;
    public const uint XIP_SRAM_START_RP2350 = 0x13ffc000;
    public const uint XIP_SRAM_END_RP2350 = 0x14000000;

    public const uint SRAM_START = 0x20000000;
    public const uint SRAM_END_RP2040 = 0x20042000;
    public const uint SRAM_END_RP2350 = 0x20082000;
    public const uint MAIN_RAM_BANKED_START = 0x21000000;
    public const uint MAIN_RAM_BANKED_END = 0x21040000;

    // Equivalent to find_binary_start in picotool
    public static uint FindBinaryStart(Uf2View view)
    {
        // These use the largest range of supported devices
        AddressRange sram = new(SRAM_START, SRAM_END_RP2350);
        AddressRange xipSram = new(XIP_SRAM_START_RP2350, XIP_SRAM_END_RP2040);

        uint result = uint.MaxValue;
        foreach (ref readonly Uf2Block block in view)
        {
            if (block.AddressRange.Contains(FLASH_START))
                return FLASH_START;

            if (sram.Contains(block.TargetAddress) || xipSram.Contains(block.TargetAddress))
            {
                if (block.TargetAddress < result || (xipSram.Contains(result) && sram.Contains(block.TargetAddress)))
                    result = block.TargetAddress;
            }
        }

        // RP2350 is used here as it has the largest ranges
        // Note that there is a bug here when the start address lies within the XIP SRAM between XIP_SRAM_END_RP2350 and XIP_SRAM_END_RP2040
        // This bug is also present in picotool, so we repeat it here. Realistically this check is kinda useless, we should just check that it isn't MaxValue.
        if (Picoboot.PBC_get_memory_type(result, model_t.rp2350) == memory_type.invalid)
            return 0;

        return result;
    }

    public static AddressRange FlashRange(model_t model)
        => model switch
        {
            model_t.rp2040 => new AddressRange(FLASH_START, FLASH_END_RP2040),
            model_t.rp2350 => new AddressRange(FLASH_START, FLASH_END_RP2350),
            // Default to biggest range
            _ => new AddressRange(FLASH_START, FLASH_END_RP2350),
        };
}
