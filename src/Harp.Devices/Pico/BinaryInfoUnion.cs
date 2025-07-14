using System.Runtime.InteropServices;

namespace Harp.Devices.Pico;

[StructLayout(LayoutKind.Explicit)]
public readonly struct BinaryInfoUnion
{
    [FieldOffset(0)] public readonly BinaryInfoHeader Core;
    [FieldOffset(0)] public readonly BinaryInfoIdAndString IdAndString;
    [FieldOffset(0)] public readonly BinaryInfoIdAndInt IdAndInt;
    // See comment in BinaryInfo's constructor before adding more members here.

    public override string ToString()
        => Core.Type switch
        {
            BinaryInfoType.ID_AND_STRING => IdAndString.ToString(),
            BinaryInfoType.ID_AND_INT => IdAndInt.ToString(),
            _ => Core.ToString(),
        };
}
