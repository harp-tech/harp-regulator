using System;

namespace PicobootConnection.LibUsb;

public sealed class LibUsbException : Exception
{
    public readonly libusb_error Error;

    public LibUsbException(libusb_error error)
        : base(error.GetMessage())
        => Error = error;

    public LibUsbException(string? messagePrefix, libusb_error error)
        : base(messagePrefix is null ? error.GetMessage() : $"{messagePrefix}: {error.GetMessage()}")
        => Error = error;
}
