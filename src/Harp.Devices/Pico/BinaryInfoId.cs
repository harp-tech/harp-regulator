namespace Harp.Devices.Pico;

public enum BinaryInfoId : uint
{
    // These are just the RP-defined values, they can be anything
    RP_PROGRAM_NAME = 0x02031c86,
    RP_PROGRAM_VERSION_STRING = 0x11a9bc3a,
    RP_PROGRAM_BUILD_DATE_STRING = 0x9da22254,
    RP_BINARY_END = 0x68f465de,
    RP_PROGRAM_URL = 0x1856239a,
    RP_PROGRAM_DESCRIPTION = 0xb6a07c19,
    RP_PROGRAM_FEATURE = 0xa1f4b453,
    RP_PROGRAM_BUILD_ATTRIBUTE = 0x4275f0d3,
    RP_SDK_VERSION = 0x5360b3ab,
    RP_PICO_BOARD = 0xb63cffbb,
    RP_BOOT2_NAME = 0x7f8882e1,
}
