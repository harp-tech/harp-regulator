using System.Runtime.InteropServices;

namespace PicobootConnection.LibUsb;

[StructLayout(LayoutKind.Sequential)]
public readonly struct libusb_context
{
    private readonly nint __opaqueHandle;
    public bool IsNull => __opaqueHandle == nint.Zero;
}
