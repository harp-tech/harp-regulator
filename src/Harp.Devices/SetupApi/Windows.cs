using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

internal static class Windows
{
    public static HANDLE INVALID_HANDLE_VALUE => (HANDLE)(-1);

    public const int MAX_PATH = 260;

    /// <remarks>
    /// This method does not actually call the Windows GetLastError function, but instead wraps Marshal.GetLastWin32Error instead.
    /// It is provided for convienence of not having to cast the return value of Marshal.GetLastWin32Error.
    /// </remarks>
    public static Win32Error GetLastError()
        => (Win32Error)Marshal.GetLastPInvokeError();
}
