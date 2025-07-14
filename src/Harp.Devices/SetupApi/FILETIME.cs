using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

[StructLayout(LayoutKind.Sequential)]
internal struct FILETIME
{
    public uint dwLowDateTime;
    public uint dwHighDateTime;
}
