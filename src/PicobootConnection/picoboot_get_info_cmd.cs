using System.Runtime.InteropServices;

namespace PicobootConnection;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct picoboot_get_info_cmd
{
    public PicobootInfoType bType;
    public byte bParam;
    public ushort wParam;
    public uint dParams0;
    public uint dParams1;
    public uint dParams2;
}
