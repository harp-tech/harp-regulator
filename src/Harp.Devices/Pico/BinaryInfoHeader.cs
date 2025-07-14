namespace Harp.Devices.Pico;

public readonly struct BinaryInfoHeader
{
    public readonly BinaryInfoType Type;
    public readonly BinaryInfoTag Tag;

    public override string ToString()
        => $"{Type} {Tag}";
}
