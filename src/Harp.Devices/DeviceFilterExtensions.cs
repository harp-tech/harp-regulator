using System;
using System.Collections.Immutable;
using System.Linq;

namespace Harp.Devices;

public static class DeviceFilterExtensions
{
    /// <summary>Checks if the device's serial number exactly matches, starts, or ends with the given partial serial number.</summary>
    /// <remarks>
    /// This method is intended to support matching serial numbers when either the user doesn't want to specify as full serial number (because it's too long)
    /// or when the serial number is truncated in some contexts (such as due to Harp only using 16 bits for R_SERIAL_NUMBER when picoboot uses 64 bits.)
    /// </remarks>
    public static bool SerialNumberPartialMatch(this Device device, string partialSerialNumber)
    {
        if (device.SerialNumber is ulong serialNumber)
        {
            string serialString = serialNumber.ToString("x");
            if (serialString.StartsWith(partialSerialNumber, StringComparison.OrdinalIgnoreCase) || serialString.EndsWith(partialSerialNumber, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static ImmutableArray<Device> Filter(this ImmutableArray<Device> devices, Func<Device, bool> predicate)
        => devices.Where(predicate).ToImmutableArray();

    /// <summary>Filters this device list using the given human-writable filter compatible with Harp Regulator's <c>upload</c> command.</summary>
    /// <param name="targetFilter">A loose filter string compatible with Harp Regulator's <c>upload</c> command.</param>
    public static ImmutableArray<Device> Filter(this ImmutableArray<Device> devices, string targetFilter)
    {
        if (targetFilter.Equals("PICOBOOT", StringComparison.OrdinalIgnoreCase))
            return devices.Filter(d => d.State == DeviceState.Bootloader);

        return devices.Filter
        (
            (device) =>
            {
                if (device.PortName?.Equals(targetFilter, StringComparison.OrdinalIgnoreCase) ?? false)
                    return true;

                if (device.SerialNumberPartialMatch(targetFilter))
                    return true;

                return false;
            }
        );
    }
}
