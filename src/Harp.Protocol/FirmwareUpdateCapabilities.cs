using System;

namespace Harp.Protocol;

[Flags]
public enum FirmwareUpdateCapabilities : uint
{
    None = 0,
    FIRMWARE_UPDATE_PICO_BOOTSEL = 1,
}
