namespace Harp.Devices.Pico;

public enum BinaryInfoType : ushort
{
    RAW_DATA = 1,
    SIZED_DATA = 2,
    BINARY_INFO_LIST_ZERO_TERMINATED = 3,
    BSON = 4,
    ID_AND_INT = 5,
    ID_AND_STRING = 6,
    BLOCK_DEVICE = 7,
    PINS_WITH_FUNC = 8,
    PINS_WITH_NAME = 9,
    NAMED_GROUP = 10,
    PTR_INT32_WITH_NAME = 11,
    PTR_STRING_WITH_NAME = 12,
    PINS64_WITH_FUNC = 13,
    PINS64_WITH_NAME = 14,
}
