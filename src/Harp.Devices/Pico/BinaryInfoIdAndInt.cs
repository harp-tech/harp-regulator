namespace Harp.Devices.Pico;
public readonly struct BinaryInfoIdAndInt
{
    public readonly BinaryInfoHeader Header;
    public readonly BinaryInfoId Id;
    public readonly int Value;

    public override string ToString()
        => $"{Header} {Id} = {Value}";
}
