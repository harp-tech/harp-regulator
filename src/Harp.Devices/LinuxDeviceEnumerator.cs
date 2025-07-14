using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Harp.Devices;

[SupportedOSPlatform("linux")]
internal static class LinuxDeviceEnumerator
{
    internal static void EnumerateDevices(ImmutableArray<Device>.Builder builder)
    {
        //TODO: Implement me
        // See docs/IdentifyingHarpDevices.md for details on expected behavior
        //
        // libusb was not vaiable on Windows due to the fact that it requires communicating with
        // the device to get things like the USB interface description string and communicating
        // with devices on Windows requires they be using the WinUSB driver. Even if it was easy to
        // switch to WinUSB temporarily (it extremely isn't), the whole point of this strategy for
        // device enumeration is to identify devices without connecting to them and swapping the
        // driver would require kicking off anything connected to the serial port.
        //
        // IIRC Linux is actually similar in that libusb changes the Kernel module which controls
        // the device so a similar approach is probably necessary. (I suspect enumeration will be a
        // bit simpler though and all of the relevant info is exposed via sysfs.)
        Trace.WriteLine("Warning: Support for enumerating Harp devices via USB descriptors is not implemneted on Linux.");
    }
}
