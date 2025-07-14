using System;
using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

[StructLayout(LayoutKind.Sequential)]
internal struct SP_DEVINFO_DATA
{
    public uint cbSize;
    public Guid ClassGuid;
    public uint DevInst;
    public nuint Reserved;
}
