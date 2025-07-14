using PicobootConnection;
using System;
using static PicobootConnection.Picoboot;

namespace Harp.Devices.Pico;

/// <remarks>This is roughly requivalent to picoboot_memory_access in picotool.</remarks>
internal sealed class PhysicalDeviceFlashReader : PicoFlashReaderBase
{
    private readonly PicobootDevice Pico;

    public override uint BinaryStart => PicoMemoryMap.FLASH_START;

    public PhysicalDeviceFlashReader(PicobootDevice pico)
        => Pico = pico;

    public unsafe override void Read(uint address, Span<byte> buffer)
    {
        if (PBC_get_memory_type(address, Pico.Model) == memory_type.flash)
            Pico.ExitXip();

        Pico.Read(address, buffer);
    }

    public override model_t ReadModel()
    {
        const uint BOOTROM_MAGIC_RP2040 = 0x01754d;
        const uint BOOTROM_MAGIC_RP2350 = 0x02754d;
        const uint BOOTROM_MAGIC_ADDR = 0x00000010;

        uint magic = Read<uint>(BOOTROM_MAGIC_ADDR);
        magic &= 0xffffff; // Ignore bootrom version
        return magic switch
        {
            BOOTROM_MAGIC_RP2040 => model_t.rp2040,
            BOOTROM_MAGIC_RP2350 => model_t.rp2350,
            _ => throw new NotSupportedException($"Unknown Pico bootroom magic '0x{magic}'"),
        };
    }

    public override string ToString()
        => "Physical PICOBOOT device reader";
}
