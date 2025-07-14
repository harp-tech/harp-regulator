namespace Harp.Devices.Pico;

public enum BinaryInfoTag : ushort
{
    // See pico-sdk/src/common/pico_binary_info/include/pico/binary_info/structure.h on info about allocating these
    RASPBERRY_PI = 0x5052, // 'R' 'P'
}
