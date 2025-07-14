using System.Runtime.InteropServices;

namespace PicobootConnection;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct picoboot_otp_cmd
{
    public ushort wRow;
    public ushort wRowCount;
    public byte bEcc;
}
