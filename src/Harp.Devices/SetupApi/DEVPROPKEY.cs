using System;
using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

[StructLayout(LayoutKind.Sequential)]
internal partial struct DEVPROPKEY
{
    public Guid fmtid;
    public uint pid;

    public DEVPROPKEY(Guid fmtid, uint pid)
    {
        this.fmtid = fmtid;
        this.pid = pid;
    }

    public DEVPROPKEY(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k, uint pid)
    {
        fmtid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
        this.pid = pid;
    }
}
