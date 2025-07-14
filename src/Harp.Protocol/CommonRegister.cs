namespace Harp.Protocol;

public enum CommonRegister : byte
{
    /// <summary>U16 - Who am I</summary>
    R_WHO_AM_I = 0,
    /// <summary>U8 - Major Hardware version</summary>
    R_HW_VERSION_H = 1,
    /// <summary>U8 - Minor Hardware version</summary>
    R_HW_VERSION_L = 2,
    /// <summary>U8 - Version of the assembled components</summary>
    R_ASSEMBLY_VERSION = 3,
    /// <summary>U8 - Major core version</summary>
    R_CORE_VERSION_H = 4,
    /// <summary>U8 - Minor core version</summary>
    R_CORE_VERSION_L = 5,
    /// <summary>U8 - Major Firmware version of the application</summary>
    R_FW_VERSION_H = 6,
    /// <summary>U8 - Minor Firmware version of the application</summary>
    R_FW_VERSION_L = 7,
    /// <summary>U32 - System timestamp: seconds</summary>
    R_TIMESTAMP_SECOND = 8,
    /// <summary>U16 - System timestamp: microseconds</summary>
    R_TIMESTAMP_MICRO = 9,
    /// <summary>U8 - Configuration of the operation mode</summary>
    R_OPERATION_CTRL = 10,
    /// <summary>U8 - Reset device and save non-volatile registers</summary>
    R_RESET_DEV = 11,
    /// <summary>U8 - Name of the device given by the user</summary>
    R_DEVICE_NAME = 12,
    /// <summary>U16 - Unique serial number of the device</summary>
    R_SERIAL_NUMBER = 13,
    /// <summary>U8 - Synchronization clock configuration</summary>
    R_CLOCK_CONFIG = 14,
    /// <summary>U8 - Adds an offset if user updates the Timestamp</summary>
    R_TIMESTAMP_OFFSET = 15,

    //TODO: These registers are not ratified but are described
    R_UID = 16,
    R_TAG = 17,

    //TODO: These need to be ratified and assigned proper common register values
    R_FIRMWARE_UPDATE_CAPABILITIES = 32,
    R_FIRMWARE_UPDATE_START_COMMAND = 33,
}
