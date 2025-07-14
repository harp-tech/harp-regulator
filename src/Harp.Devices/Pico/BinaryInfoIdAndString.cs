namespace Harp.Devices.Pico;

public readonly struct BinaryInfoIdAndString
{
    public readonly BinaryInfoHeader Header;
    public readonly BinaryInfoId Id;
    public readonly uint ValueAddress;

    public override string ToString()
        => $"{Header} {Id} @ 0x{ValueAddress:X4}";
}
