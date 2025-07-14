using System.Runtime.InteropServices;

namespace PicobootConnection;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct picoboot_cmd_status
{
    public uint dToken;
    public picoboot_status dStatusCode;
    public byte bCmdId;
    public byte bInProgress;
    public fixed byte _pad[6];
}
