using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SP_DEVINSTALL_PARAMS_W
{
    public uint cbSize;
    public DevInstallFlags Flags;
    public DevInstallFlagsEx FlagsEx;
    public HWND hwndParent;
    public void* InstallMsgHandler;
    public void* InstallMsgHandlerContext;
    public void* FileQueue;
    public nuint ClassInstallReserved;
    public uint Reserved;
    public fixed char DriverPath[Windows.MAX_PATH];
}
