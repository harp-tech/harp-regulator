using Harp.Devices.Pico;
using Harp.Devices.SetupApi;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PicobootConnection.LibUsb;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using static Harp.Devices.SetupApi.Globals;

namespace Harp.Devices;

[SupportedOSPlatform("windows")]
internal unsafe static class WindowsDeviceEnumerator
{
    private static Device? TryCreate(ref LibUsbDeviceList? libUsbDevices, HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo)
    {
        Device result;

        // Use the device instance ID to determine the device kind
        // Realistically we're actually just trying to check the VID/PID, but Windows doesn't actually expose them directly.
        // Even though it feels a bit janky, libusb and other references I've seen just parse the instance ID.
        // We don't need the actual values, so we just check the prefix.
        string? instanceId = TryGetDevicePropertyString(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_InstanceId);
        if (instanceId is null)
            return null;

        Guid? classGuid = TryGetDevicePropertyGuid(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_ClassGuid);

        if (instanceId.StartsWith(UsbIds.RaspberryPiFoundation, StringComparison.Ordinal))
        {
            if (instanceId.StartsWith(UsbIds.OnlineRP2040, StringComparison.Ordinal) || instanceId.StartsWith(UsbIds.OnlineRP2350, StringComparison.Ordinal))
            {
                // Only handle the serial port interface (not the USB composite root or the reset interface.)
                if (classGuid != GUID_DEVCLASS_PORTS)
                    return null;

                Trace.WriteLine($"USB device '{instanceId}' is an online Pico device.");
                result = new Device()
                {
                    Confidence = DeviceConfidence.Low,
                    Kind = DeviceKind.Pico,
                    State = DeviceState.Online,
                    Source = $"Pico USB Serial Port - {instanceId}",
                };

                TryAddIdentityFromUsbDescriptor();
            }
            else if (instanceId.StartsWith(UsbIds.BootselRP2040, StringComparison.Ordinal) || instanceId.StartsWith(UsbIds.BootselRP2350, StringComparison.Ordinal))
            {
                // Ensure changes to the logic around the RP2040 BOOTSEL devices is propagated to DriverHelper.

                // Only handle the Picoboot interface (not the USB composite root or the mass storage device.)
                // The Picoboot interface is designated by the vendor-specific interface class (0xFF)
                // This is how Picotool identifies it:
                // https://github.com/raspberrypi/picotool/blob/de8ae5ac334e1126993f72a5c67949712fd1e1a4/picoboot_connection/picoboot_connection.c#L163
                // And how each bootrom defines it too:
                // https://github.com/raspberrypi/pico-bootrom-rp2040/blob/ef22cd8ede5bc007f81d7f2416b48db90f313434/bootrom/usb_boot_device.c#L95
                // https://github.com/raspberrypi/pico-bootrom-rp2350/blob/fd6104450fa8f55c11c0c9b54dbc69a27537130f/src/nsboot/nsboot_usb_client.c#L101
                // In theory we could check the subclass is 0x00 too, but Picotool doesn't so we don't either.
                // (It's not 100% clear what that EP address check is actually doing, but I don't think it's important for our needs.)
                bool found = false;
                foreach (string compatibleId in TryGetDevicePropertyStringList(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_CompatibleIds))
                {
                    if (compatibleId == UsbIds.PicobootCompatibleId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return null;

                Trace.WriteLine($"USB device '{instanceId}' is an Pico device in BOOTSEL mode.");
                result = new Device()
                {
                    Confidence = DeviceConfidence.Low,
                    Kind = DeviceKind.Pico,
                    State = DeviceState.Bootloader,
                    Source = $"Picoboot USB Device - {instanceId}",
                };

                if (PicobootDevice.TryOpen(libUsbDevices ??= new(), deviceList, deviceInfo, instanceId) is PicobootDevice picobootDevice)
                    result = result.WithMetadataFromPicobootDevice(picobootDevice);
            }
            else
            {
                Trace.WriteLine($"USB device with instance ID '{instanceId}' is a Raspberry Pi Foundation device, but not one we recognize.");
                return null;
            }
        }
        else if (instanceId.StartsWith(UsbIds.GenericFtdi, StringComparison.Ordinal) || instanceId.StartsWith(UsbIds.GenericFtdi2, StringComparison.Ordinal))
        {
            // Only handle the serial port interface
            if (classGuid != GUID_DEVCLASS_PORTS)
                return null;

            Trace.WriteLine($"USB device '{instanceId}' is an FTDI serial port interface.");
            result = new Device()
            {
                Confidence = DeviceConfidence.Low,
                Kind = DeviceKind.FTDI,
                State = DeviceState.Online,
                Source = $"FTDI USB Device - {instanceId}",
            };

            // If we're able to populate a WhoAmI from the FTDI device's description we assume it's a ATxmega device
            if (TryAddIdentityFromUsbDescriptor() && result.WhoAmI is not null)
            {
                Trace.WriteLine($"USB device '{instanceId}' is had Harp metadata, assuming it's an ATxmega device.");
                result = result with { Kind = DeviceKind.ATxmega };
            }
        }
        else
        {
            // Unrecognized USB serial port
            if (classGuid == GUID_DEVCLASS_PORTS && instanceId.StartsWith(@"USB\", StringComparison.Ordinal))
                Trace.WriteLine($"USB device '{instanceId}' is a port device, but not one we recognize as a potential Harp device.");

            return null;
        }

        // If the USB device is a port device, get its port name
        if (classGuid == GUID_DEVCLASS_PORTS)
            TryAddPortNameFromDevice();

        // Report device driver issues
        // Check if the driver is installed
        bool hasProblemStatus = TryGetDevicePropertyT(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_ProblemStatus, DEVPROPTYPE.NTSTATUS, out uint problemStatus);
        bool hasProblemCode = TryGetDevicePropertyT(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_ProblemCode, DEVPROPTYPE.UINT32, out CM_PROB problemCode);
        const uint minWarningStatus = 0x80000000;
        if ((hasProblemStatus && problemStatus > minWarningStatus) || (hasProblemCode && problemCode != CM_PROB.None))
        {
            //TODO: It'd be nice if we used FormatMessage to get the message associated with the status
            // https://learn.microsoft.com/en-us/windows-hardware/drivers/install/devprop-type-ntstatus#retrieving-the-descriptive-text-for-a-ntstatus-error-code-value
            Trace.WriteLine($"'{result.PortName ?? instanceId}' is in an error state. Error {problemCode.ToString()}, Status = 0x{problemStatus:x4}");
            result = result with { State = DeviceState.DriverError };
        }

        return result;

        bool TryAddPortNameFromDevice()
        {
            HKEY unsafeHandle = SetupDiOpenDevRegKey(deviceList, deviceInfo, DeviceInstanceConfigurationScope.DICS_FLAG_GLOBAL, 0, DeviceInstanceRegistryKey.DIREG_DEV, REGSAM.KEY_QUERY_VALUE);
            if (unsafeHandle == Windows.INVALID_HANDLE_VALUE)
            {
                Win32Error error = Windows.GetLastError();
                Trace.WriteLine($"Failed to open device configuration registry key for '{instanceId}': {error} {error.GetMessage()}");
                return false;
            }

            using SafeRegistryHandle registryHandle = new((IntPtr)unsafeHandle, ownsHandle: true);
            using RegistryKey key = RegistryKey.FromHandle(registryHandle);

            try
            {
                if (key.GetValue("PortName")?.ToString() is string portName)
                {
                    result = result with { PortName = portName };
                    return true;
                }
            }
            catch (Exception ex)
            { Trace.WriteLine($"Failed to get port name for '{instanceId}': {ex}"); }

            return false;
        }

        bool TryAddIdentityFromUsbDescriptor()
        {
            Debug.Assert(result.Confidence < DeviceConfidence.High && result.WhoAmI is null);

            string? deviceDescription = TryGetDevicePropertyString(deviceList, deviceInfo, DEVPROPKEY.DEVPKEY_Device_BusReportedDeviceDesc);
            if (deviceDescription is null)
                return false;

            result = result.WithMetadataFromUsbDescription(deviceDescription);
            return true;
        }
    }

    internal static void EnumerateDevices(ImmutableArray<Device>.Builder builder)
    {
        // Note: Avoid any temptation to replace this with libusb to make it automatically cross-platform
        // libusb reads things like the USB descriptor strings by communicating with the device, which we're actively trying to avoid
        // Additionally, libusb requires the device to be using the generic WinUSB driver, so it can't easily communicate with online Harp devices.
        HDEVINFO deviceList = (HDEVINFO)Windows.INVALID_HANDLE_VALUE;
        LibUsbDeviceList? libUsbDevices = null;
        try
        {
            deviceList = SetupDiGetClassDevsW(null, null, null, DeviceInstanceGetClassFlags.DIGCF_ALLCLASSES | DeviceInstanceGetClassFlags.DIGCF_PRESENT);
            if (deviceList == Windows.INVALID_HANDLE_VALUE)
            {
                Trace.WriteLine($"Failed to enumerate USB devices: {new Win32Exception().Message}");
                return;
            }

            for (uint index = 0; ; index++)
            {
                SP_DEVINFO_DATA deviceInfo = new()
                {
                    cbSize = (uint)sizeof(SP_DEVINFO_DATA),
                };

                if (!SetupDiEnumDeviceInfo(deviceList, index, &deviceInfo))
                {
                    Win32Error lastError = Windows.GetLastError();
                    if (lastError != Win32Error.ERROR_NO_MORE_ITEMS)
                        Trace.WriteLine($"Failed to enumerate device #{index} from the device set: {lastError} {lastError.GetMessage()}");

                    return;
                }

                if (TryCreate(ref libUsbDevices, deviceList, &deviceInfo) is Device newDevice)
                    builder.Add(newDevice);
            }
        }
        finally
        {
            libUsbDevices?.Dispose();

            if (deviceList != Windows.INVALID_HANDLE_VALUE)
                SetupDiDestroyDeviceInfoList(deviceList).AssertTrue();
        }
    }
}
