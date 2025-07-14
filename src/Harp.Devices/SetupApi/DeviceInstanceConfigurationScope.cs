using System;

namespace Harp.Devices.SetupApi;

[Flags]
internal enum DeviceInstanceConfigurationScope
{
    DICS_FLAG_GLOBAL = 0x00000001,
    DICS_FLAG_CONFIGSPECIFIC = 0x00000002,
    DICS_FLAG_CONFIGGENERAL = 0x00000004,
}
