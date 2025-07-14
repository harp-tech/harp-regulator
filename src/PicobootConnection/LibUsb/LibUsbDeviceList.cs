using System;
using static PicobootConnection.LibUsb.Globals;

namespace PicobootConnection.LibUsb;

public unsafe sealed class LibUsbDeviceList : IDisposable
{
    private readonly libusb_device* List;
    private readonly int Length;
    public ReadOnlySpan<libusb_device> Devices => new(List, Length);

    public LibUsbDeviceList(libusb_context context)
    {
        libusb_device* list;
        nint length = libusb_get_device_list(context, &list);
        if (length < 0)
            checked((libusb_error)length).Throw();

        List = list;
        Length = checked((int)length);
    }

    public LibUsbDeviceList()
        : this(LibusbManager.Context)
    { }

    public ReadOnlySpan<libusb_device>.Enumerator GetEnumerator()
        => Devices.GetEnumerator();

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (List is not null)
            libusb_free_device_list(List, 1);
    }

    ~LibUsbDeviceList()
        => Dispose();
}
