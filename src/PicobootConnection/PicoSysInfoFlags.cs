using System;

namespace PicobootConnection;

/// <remarks>See https://datasheets.raspberrypi.com/rp2350/rp2350-datasheet.pdf#api-get_sys_info</remarks>
[Flags]
public enum PicoSysInfoFlags : uint
{
    SYS_INFO_CHIP_INFO = 0x0001,
    SYS_INFO_CRITICAL = 0x0002,
    SYS_INFO_CPU_INFO = 0x0004,
    SYS_INFO_FLASH_DEV_INFO = 0x0008,
    SYS_INFO_BOOT_RANDOM = 0x0010,
    [Obsolete("Not supported")] SYS_INFO_NONCE = 0x0020,
    SYS_INFO_BOOT_INFO = 0x0040,
}
