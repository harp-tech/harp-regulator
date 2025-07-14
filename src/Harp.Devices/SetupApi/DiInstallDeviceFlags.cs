using System;

namespace Harp.Devices.SetupApi;

[Flags]
internal enum DiInstallDeviceFlags : uint
{
    None = 0,
    /// <summary>Show search UI if no drivers can be found.</summary>
    DIIDFLAG_SHOWSEARCHUI = 0x00000001,
    /// <summary>Do NOT show the finish install UI.</summary>
    DIIDFLAG_NOFINISHINSTALLUI = 0x00000002,
    /// <summary>Install the NULL driver on this device.</summary>
    DIIDFLAG_INSTALLNULLDRIVER = 0x00000004,
    /// <summary>Install any extra INFs specified via CopyInf directive.</summary>
    DIIDFLAG_INSTALLCOPYINFDRIVERS = 0x00000008,
}
