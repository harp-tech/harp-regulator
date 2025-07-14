using Harp.Devices.SetupApi;
using PicobootConnection;
using PicobootConnection.LibUsb;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using static Harp.Devices.SetupApi.Globals;
using static PicobootConnection.LibUsb.Globals;
using static PicobootConnection.Picoboot;

namespace Harp.Devices.Pico;

partial class PicobootDevice
{
    [SupportedOSPlatform("windows")]
    internal static unsafe PicobootDevice? TryOpen(LibUsbDeviceList libUsbDevices, HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, string instanceId)
    {
        Trace.WriteLine("==============================================================================");
        Trace.WriteLine($"Seaching for libusb device corresponding to '{instanceId}'...");

        // WindowsEnumerator enumerates the PICOBOOT USB device directly, but libusb only enumerates the root composite devices
        // As such we use the instance ID of the parent device to find the actual device
        string? searchInstanceId = TryGetDevicePropertyString(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_Parent);
        if (searchInstanceId is null)
        {
            Trace.WriteLine($"Could not determine the parent instance ID for '{instanceId}'! Will try searching for it directly.");
            searchInstanceId = instanceId;
        }
        else if (searchInstanceId.StartsWith(UsbIds.BootselRP2040) || searchInstanceId.StartsWith(UsbIds.BootselRP2350))
        { Trace.WriteLine($"Got '{searchInstanceId}' as BOOTSEL composite device parent of '{instanceId}'"); }
        else
        {
            // This fallback is very unlikely to ever be used or make sense, but might as well try.
            Trace.WriteLine($"Parent of provided instance id '{instanceId}' is '{searchInstanceId}', but that doesn't appear to be a BOOTSEL composite device! Will try searching for the provided instance ID directly.");
            searchInstanceId = instanceId;
        }

        // Find the device corresponding to the SetupAPI device
        libusb_device foundDevice = default;
        string identity = searchInstanceId;
        {
            Span<char> deviceIdStringBuffer = new char[1024];
            foreach (libusb_device device in libUsbDevices)
            {
                // libusb uses SetupDiGetDeviceInstanceIdA to get the instance ID, which is actually not super ideal
                // In fact this might not actually be correct on all systems since this should probably be using the encoding for the system locale rather than UTF8
                // (AFAIK there is not any sort of way to get an Encoding for the system code page in .NET, so doing this "correctly" is not straightforward.)
                ReadOnlySpan<byte> deviceId = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(PBC_GetUsbInstanceId(device));
                int maxChars = Encoding.UTF8.GetMaxCharCount(deviceId.Length);
                if (deviceIdStringBuffer.Length < maxChars)
                    deviceIdStringBuffer = new char[maxChars];
                ReadOnlySpan<char> deviceIdString = deviceIdStringBuffer.Slice(0, Encoding.UTF8.GetChars(deviceId, deviceIdStringBuffer));

                // The casing of the two instance IDs is not always consistent for whatever reason, so we ignore it.
                if (deviceIdString.Equals(searchInstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    // Neither of these values are actually useful for our purposesd (see comments in native implementation of PBC_GetUsbInstanceId)
                    // However we print them so that folks can relate the verbose output to what they're seeing in picotool for debugging purposes.
                    byte deviceAddress = libusb_get_device_address(device);
                    byte busNumber = libusb_get_bus_number(device);
                    Trace.WriteLine($"Found '{searchInstanceId}' as libusb device address {deviceAddress} on bus {busNumber}.");
                    identity = $"Picoboot interface for '{instanceId}' as libusb device address {deviceAddress} on bus {busNumber} via '{searchInstanceId}'";

                    foundDevice = device;
                    break;
                }
            }
        }

        if (foundDevice.IsNull)
        {
            Trace.WriteLine($"Could not find libusb device corresponding to '{instanceId}'");
            Trace.WriteLine("==============================================================================");
            return null;
        }

        // Initialize the PICOBOOT device
        libusb_device_handle handle = default;
        try
        {
            model_t model;
            byte* serial = stackalloc byte[1];
            serial[0] = 0;
            picoboot_device_result result = picoboot_open_device(foundDevice, &handle, &model, -1, -1, serial);

            if (result != picoboot_device_result.dr_vidpid_bootrom_ok)
            {
                Trace.WriteLine($"Could not open picoboot device: {result}");
                return null;
            }

            PicobootDevice picobootDevice = new(identity, model, handle);
            handle = default; // PicobootDevice owns the handle now
            return picobootDevice;
        }
        finally
        {
            if (!handle.IsNull)
                libusb_close(handle);
            Trace.WriteLine("==============================================================================");
        }
    }
}
