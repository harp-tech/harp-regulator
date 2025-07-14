namespace Harp.Devices;

public enum DeviceState
{
    Unknown,
    /// <summary>Indicates that the device was detected, but the operating system driver is not configured or experiencing problems.</summary>
    DriverError,
    /// <summary>Indicates that the device is waiting in bootloader mode.</summary>
    /// <remarks>For Pico devices, this indicates that the device is in BOOTSEL mode.</remarks>
    Bootloader,
    /// <summary>Indicates that the device is running Harp firmware.</summary>
    Online,
}
