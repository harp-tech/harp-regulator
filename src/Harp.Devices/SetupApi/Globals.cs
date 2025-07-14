using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Harp.Devices.SetupApi;

[SupportedOSPlatform("windows")]
internal unsafe static partial class Globals
{
    public static readonly Guid GUID_DEVCLASS_PORTS = new(0x4d36e978, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial HDEVINFO SetupDiGetClassDevsW(Guid* classGuid, char* enumerator, HWND hwndParent, DeviceInstanceGetClassFlags flags);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiDestroyDeviceInfoList(HDEVINFO DeviceInfoSet);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, uint MemberIndex, SP_DEVINFO_DATA* DeviceInfoData);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiGetDevicePropertyW
    (
        HDEVINFO DeviceInfoSet,
        SP_DEVINFO_DATA* DeviceInfoData,
        DEVPROPKEY* PropertyKey,
        DEVPROPTYPE* PropertyType,
        byte* PropertyBuffer,
        uint PropertyBufferSize,
        uint* RequiredSize,
        uint Flags
    );

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial HKEY SetupDiOpenDevRegKey
    (
        HDEVINFO DeviceInfoSet,
        SP_DEVINFO_DATA* DeviceInfoData,
        DeviceInstanceConfigurationScope Scope,
        uint HwProfile,
        DeviceInstanceRegistryKey KeyType,
        REGSAM samDesired
    );

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiGetDeviceInstallParamsW(HDEVINFO DeviceInfoSet, SP_DEVINFO_DATA* DeviceInfoData, SP_DEVINSTALL_PARAMS_W* DeviceInstallParams);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiSetDeviceInstallParamsW(HDEVINFO DeviceInfoSet, SP_DEVINFO_DATA* DeviceInfoData, SP_DEVINSTALL_PARAMS_W* DeviceInstallParams);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiBuildDriverInfoList(HDEVINFO DeviceInfoSet, SP_DEVINFO_DATA* DeviceInfoData, DiDriverType DriverType);

    [LibraryImport("SetupAPI.dll", SetLastError = true)]
    public static partial BOOL SetupDiEnumDriverInfoW
    (
        HDEVINFO DeviceInfoSet,
        SP_DEVINFO_DATA* DeviceInfoData,
        DiDriverType DriverType,
        uint MemberIndex,
        SP_DRVINFO_DATA_W* DriverInfoData
    );

    [LibraryImport("Newdev.dll", SetLastError = true)]
    public static partial BOOL DiInstallDevice
    (
        HWND hwndParent,
        HDEVINFO DeviceInfoSet,
        SP_DEVINFO_DATA* DeviceInfoData,
        SP_DRVINFO_DATA_W* DriverInfoData,
        DiInstallDeviceFlags Flags,
        BOOL* NeedReboot
    );
}
