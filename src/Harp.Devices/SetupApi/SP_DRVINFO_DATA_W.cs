using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SP_DRVINFO_DATA_W // AKA SP_DRVINFO_DATA_V2_W
{
    public uint cbSize;
    public uint DriverType;
    public nuint Reserved;
    public const int LINE_LEN = 256;
    public fixed char Description[LINE_LEN];
    public fixed char MfgName[LINE_LEN];
    public fixed char ProviderName[LINE_LEN];
    public FILETIME DriverDate;
    public ulong DriverVersion;
}
