using System.Runtime.InteropServices;

namespace PicobootConnection;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct picoboot_reboot2_cmd
{
    public PicobootReboot2Flags dFlags;
    public uint dDelayMS;
    public uint dParam0;
    public uint dParam1;
}
