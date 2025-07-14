using System.Runtime.InteropServices;

namespace PicobootConnection.LibUsb;

public unsafe static partial class Globals
{
    [LibraryImport("libusb-1.0")]
    public static partial libusb_error libusb_init(libusb_context* ctx);

    [LibraryImport("libusb-1.0")]
    public static partial void libusb_exit(libusb_context ctx);

    [LibraryImport("libusb-1.0")]
    public static partial nint libusb_get_device_list(libusb_context ctx, libusb_device** list);

    [LibraryImport("libusb-1.0")]
    public static partial void libusb_free_device_list(libusb_device* list, int unref_devices);

    [LibraryImport("libusb-1.0")]
    public static partial void libusb_close(libusb_device_handle dev_handle);

    [LibraryImport("libusb-1.0")]
    public static partial byte libusb_get_bus_number(libusb_device dev);

    [LibraryImport("libusb-1.0")]
    public static partial byte libusb_get_device_address(libusb_device dev);
}
