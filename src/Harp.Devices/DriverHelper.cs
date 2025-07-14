using Harp.Devices.SetupApi;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using static Harp.Devices.SetupApi.Globals;

namespace Harp.Devices;

public unsafe static class DriverHelper
{
    /// <summary>Automatically installs WinUSB for any RP2040 BOOTSEL devices connected to the system.</summary>
    /// <returns>True if all drivers installed successfully, false otherwise.</returns>
    [SupportedOSPlatform("windows")]
    public static bool InstallWinUSB(out int successCount, out int failCount, out bool restartRecommended)
    {
        // The logic for identifying devices in this method must match WindowsDeviceEnumerator!

        HDEVINFO deviceList = (HDEVINFO)Windows.INVALID_HANDLE_VALUE;
        try
        {
            deviceList = SetupDiGetClassDevsW(null, null, null, DeviceInstanceGetClassFlags.DIGCF_ALLCLASSES | DeviceInstanceGetClassFlags.DIGCF_PRESENT);
            if (deviceList == Windows.INVALID_HANDLE_VALUE)
            {
                Console.Error.WriteLine($"Failed to enumerate USB devices: {new Win32Exception().Message}");
                successCount = 0;
                failCount = 0;
                restartRecommended = false;
                return false;
            }

            successCount = 0;
            failCount = 0;
            restartRecommended = false;
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

                    return failCount == 0;
                }

                string? instanceId = TryGetDevicePropertyString(deviceList, &deviceInfo, DEVPROPKEY.DEVPKEY_Device_InstanceId);
                if (instanceId is null)
                    continue;

                // Only the RP2040 needs to be checked as they fixed the missing WinUSB hint descriptor for the RP2350
                // https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/automatic-installation-of-winusb
                // https://github.com/raspberrypi/pico-bootrom-rp2350/blob/fd6104450fa8f55c11c0c9b54dbc69a27537130f/src/nsboot/nsboot_usb_client.c#L212-L218
                if (!instanceId.StartsWith(UsbIds.BootselRP2040, StringComparison.Ordinal))
                    continue;

                // Identify the PICOBOOT interface via the vendor-specific interface class
                // This logic should match WindowsDeviceEnumerator!
                bool found = false;
                foreach (string compatibleId in TryGetDevicePropertyStringList(deviceList, &deviceInfo, DEVPROPKEY.DEVPKEY_Device_CompatibleIds))
                {
                    if (compatibleId == UsbIds.PicobootCompatibleId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    continue;

                // Only consider devices with problems
                //TODO: Should we filter on the specific problem status too?
                if (!TryGetDevicePropertyT(deviceList, &deviceInfo, DEVPROPKEY.DEVPKEY_Device_HasProblem, DEVPROPTYPE.BOOLEAN, out byte hasProblem) || hasProblem == 0)
                    continue;

                // If we reached this point we should install WinUSB for this device
                if (InstallWinUSB(instanceId, deviceList, &deviceInfo, ref restartRecommended))
                    successCount++;
                else
                    failCount++;
            }
        }
        finally
        {
            if (deviceList != Windows.INVALID_HANDLE_VALUE)
                SetupDiDestroyDeviceInfoList(deviceList).AssertTrue();
        }
    }

    /// <remarks>
    /// This method essentially performs the same actions as the manual steps listed here:
    /// https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/winusb-installation#installing-winusb-by-specifying-the-system-provided-device-class
    /// </remarks>
    [SupportedOSPlatform("windows")]
    private static bool InstallWinUSB(string instanceId, HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, ref bool restartRecommended)
    {
        Trace.WriteLine($"Attempting to install WinUSB for '{instanceId}'");

        // Configure the DeviceInterfaceGUID
        // Note that unlike the linked instructions above, we do this *before* installing the driver
        // This allows the device to work without unplugging and replugging it
        {
            HKEY unsafeHandle = SetupDiOpenDevRegKey
            (
                deviceList,
                deviceInfo,
                DeviceInstanceConfigurationScope.DICS_FLAG_GLOBAL,
                0,
                DeviceInstanceRegistryKey.DIREG_DEV,
                REGSAM.KEY_QUERY_VALUE | REGSAM.KEY_SET_VALUE
            );

            if (unsafeHandle == Windows.INVALID_HANDLE_VALUE)
            {
                Win32Error error = Windows.GetLastError();
                Console.Error.WriteLine($"Failed to open device configuration registry key for '{instanceId}': {error} {error.GetMessage()}");
                return false;
            }

            using SafeRegistryHandle registryHandle = new((IntPtr)unsafeHandle, ownsHandle: true);
            using RegistryKey key = RegistryKey.FromHandle(registryHandle);
            if (!TryConfigureDeviceInterfaceGuid(instanceId, key))
                return false;

            static bool TryConfigureDeviceInterfaceGuid(string instanceId, RegistryKey key)
            {
                const string singleGuidName = "DeviceInterfaceGUID";
                const string multipleGuidName = "DeviceInterfaceGUIDs";

                try
                {
                    if (key.GetValue(multipleGuidName) is string[] existingGuids)
                    {
                        Trace.WriteLine($"Device has multi-value {multipleGuidName} listing, this is very unusual!");
                        if (Debugger.IsAttached)
                            Debugger.Break();

                        if (existingGuids.Length == 0)
                        { Trace.WriteLine($"Device has a {multipleGuidName} value with no entries. It will be deleted."); }
                        else
                        {
                            bool haveBadGuids = false;
                            foreach (string existingGuid in existingGuids)
                            {
                                if (!Guid.TryParseExact(existingGuid, "B", out _))
                                {
                                    Trace.WriteLine($"Device GUID '{existingGuid}' is invalid.");
                                    haveBadGuids = true;
                                }
                            }

                            if (haveBadGuids)
                            { Trace.WriteLine($"More of more GUIDs were invalid, {multipleGuidName} will be deleted."); }
                            else
                            {
                                Trace.WriteLine($"All GUID(s) in the {multipleGuidName} list appear to be valid, they will be left alone.");
                                return true;
                            }
                        }

                        key.DeleteValue(multipleGuidName);
                    }

                    object? existingValue = key.GetValue(singleGuidName);
                    if (existingValue is not null)
                    {
                        RegistryValueKind valueKind = key.GetValueKind(singleGuidName);
                        if (valueKind != RegistryValueKind.String)
                        { Trace.WriteLine($"Device has a {singleGuidName}, but is a {valueKind}, but we expected a string. Will reinitialize."); }
                        else if (!Guid.TryParseExact(existingValue.ToString(), "B", out _))
                        { Trace.WriteLine($"Device has a {singleGuidName}, but its value '{existingValue}' is malformed. Will reinitialize."); }
                        else
                        {
                            Trace.WriteLine($"Device already has an existing valid {singleGuidName}, it will be left alone.");
                            return true;
                        }
                    }

                    Guid guid = Guid.NewGuid();
                    key.SetValue(singleGuidName, guid.ToString("B"), RegistryValueKind.String);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to configure {singleGuidName} for {instanceId}: {ex}");
                    return false;
                }
            }
        }

        // Configure the device to enumerate WinUSB
        {
            SP_DEVINSTALL_PARAMS_W installParams = new()
            {
                cbSize = checked((uint)sizeof(SP_DEVINSTALL_PARAMS_W)),
            };

            if (!SetupDiGetDeviceInstallParamsW(deviceList, deviceInfo, &installParams))
            {
                Win32Error error = Windows.GetLastError();
                Console.Error.WriteLine($"Failed to get device install parameters for {instanceId}: {error.GetMessage()}");
                return false;
            }

            installParams.Flags |= DevInstallFlags.DI_ENUMSINGLEINF;
            installParams.FlagsEx |= DevInstallFlagsEx.DI_FLAGSEX_ALLOWEXCLUDEDDRVS;
            Span<char> driverPath = new(installParams.DriverPath, Windows.MAX_PATH);
            "winusb.inf\0".CopyTo(driverPath);

            if (SetupDiSetDeviceInstallParamsW(deviceList, deviceInfo, &installParams))
            { Trace.WriteLine("Configured device to enumerate WinUSB."); }
            else
            {
                Win32Error error = Windows.GetLastError();
                Console.Error.WriteLine($"Failed to configure device install parameters for {instanceId}: {error.GetMessage()}");
                return false;
            }
        }

        // Find WinUSB for this device
        SP_DRVINFO_DATA_W winUsbDriverInfo;
        {
            if (!SetupDiBuildDriverInfoList(deviceList, deviceInfo, DiDriverType.SPDIT_CLASSDRIVER))
            {
                Win32Error error = Windows.GetLastError();
                Console.Error.WriteLine($"Failed to build the device driver info list for {instanceId}: {error.GetMessage()}");
                return false;
            }

            bool haveDriver = false;
            uint index = 0;
            Trace.WriteLine("Enumerating candidate drivers for the device...");
            while (true)
            {
                SP_DRVINFO_DATA_W driverInfo = new() { cbSize = checked((uint)sizeof(SP_DRVINFO_DATA_W)) };
                if (!SetupDiEnumDriverInfoW(deviceList, deviceInfo, DiDriverType.SPDIT_CLASSDRIVER, index, &driverInfo))
                {
                    Win32Error error = Windows.GetLastError();
                    if (error == Win32Error.ERROR_NO_MORE_ITEMS)
                        break;

                    Console.Error.WriteLine($"Failed to enumerate driver #{index} for {instanceId}: {error.GetMessage()}");
                    return false;
                }

                ReadOnlySpan<char> description = new(driverInfo.Description, SP_DRVINFO_DATA_W.LINE_LEN);
                description = description.SliceNullTerminated();
                Trace.WriteLine($"[{index}] '{description}'");

                if (!haveDriver && description.SequenceEqual("WinUsb Device"))
                {
                    Trace.WriteLine("    Driver selected to be WinUSB!");
                    winUsbDriverInfo = driverInfo;
                    haveDriver = true;
                }
                else if (index == 0)
                {
                    // Save the 0th driver as the selected driver as a fallback
                    winUsbDriverInfo = driverInfo;
                }

                index++;
            }

            if (!haveDriver)
            {
                if (index > 0)
                { Trace.WriteLine($"Could not find vanilla WinUSB! Using the 0th one."); }
                else
                {
                    Console.Error.WriteLine($"Failed to enumerate any WinUSB drivers for drive {instanceId}!");
                    return false;
                }
            }
        }

        // Install WinUSB
        BOOL needReboot = false;
        if (DiInstallDevice(null, deviceList, deviceInfo, &winUsbDriverInfo, DiInstallDeviceFlags.None, &needReboot))
        {
            // In practice, Windows will frequently say a reboot is required when it really isn't so we use the wording "recommended" over "needed".
            if (needReboot)
                restartRecommended = true;

            return true;
        }
        else
        {
            Win32Error error = Windows.GetLastError();
            Console.Error.WriteLine($"Failed to install WinUSB for {instanceId}: {error.GetMessage()}");
            return false;
        }
    }
}
