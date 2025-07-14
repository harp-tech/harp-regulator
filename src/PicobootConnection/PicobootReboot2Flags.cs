using System;

namespace PicobootConnection;

[Flags]
public enum PicobootReboot2Flags : uint
{
    /// <summary>param0 = diagnostic partition</summary>
    REBOOT2_FLAG_REBOOT_TYPE_NORMAL = 0x0,
    /// <summary>param0 = gpio_pin_number, param1 = flags</summary>
    REBOOT2_FLAG_REBOOT_TYPE_BOOTSEL = 0x2,
    /// <summary>param0 = image_region_base, param1 = image_region_size</summary>
    REBOOT2_FLAG_REBOOT_TYPE_RAM_IMAGE = 0x3,
    /// <summary>param0 = update_base</summary>
    REBOOT2_FLAG_REBOOT_TYPE_FLASH_UPDATE = 0x4,
    REBOOT2_FLAG_REBOOT_TYPE_PC_SP = 0xd,

    // These can be added to the above:
    REBOOT2_FLAG_REBOOT_TO_ARM = 0x10,
    REBOOT2_FLAG_REBOOT_TO_RISCV = 0x20,
    REBOOT2_FLAG_NO_RETURN_ON_SUCCESS = 0x100,
}
