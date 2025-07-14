using PicobootConnection;

namespace Harp.Devices;

internal static class UsbIds
{
    internal static readonly string RaspberryPiFoundation = $@"USB\VID_{Picoboot.VENDOR_ID_RASPBERRY_PI:X4}";
    internal static readonly string OnlineRP2040 = @$"{RaspberryPiFoundation}&PID_{Picoboot.PRODUCT_ID_RP2040_STDIO_USB:X4}";
    internal static readonly string OnlineRP2350 = @$"{RaspberryPiFoundation}&PID_{Picoboot.PRODUCT_ID_STDIO_USB:X4}";
    internal static readonly string BootselRP2040 = @$"{RaspberryPiFoundation}&PID_{Picoboot.PRODUCT_ID_RP2040_USBBOOT:X4}";
    internal static readonly string BootselRP2350 = @$"{RaspberryPiFoundation}&PID_{Picoboot.PRODUCT_ID_RP2350_USBBOOT:X4}";
    internal static readonly string GenericFtdi = @"FTDIBUS\VID_0403";
    // I don't think this is ever used by normal FTDI drivers for the actual serial port, but checking just in case.
    internal static readonly string GenericFtdi2 = @"USB\VID_0403";

    internal static readonly string PicobootCompatibleId = @"USB\Class_ff";
}
