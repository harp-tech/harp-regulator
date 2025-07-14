using System;

namespace PicobootConnection.LibUsb;

public enum libusb_error : int
{
    LIBUSB_SUCCESS = 0,
    LIBUSB_ERROR_IO = -1,
    LIBUSB_ERROR_INVALID_PARAM = -2,
    LIBUSB_ERROR_ACCESS = -3,
    LIBUSB_ERROR_NO_DEVICE = -4,
    LIBUSB_ERROR_NOT_FOUND = -5,
    LIBUSB_ERROR_BUSY = -6,
    LIBUSB_ERROR_TIMEOUT = -7,
    LIBUSB_ERROR_OVERFLOW = -8,
    LIBUSB_ERROR_PIPE = -9,
    LIBUSB_ERROR_INTERRUPTED = -10,
    LIBUSB_ERROR_NO_MEM = -11,
    LIBUSB_ERROR_NOT_SUPPORTED = -12,
    LIBUSB_ERROR_OTHER = -99,
}

public static class libusb_errorExtrensions
{
    public static string GetMessage(this libusb_error error)
        => error switch
        {
            libusb_error.LIBUSB_SUCCESS => "Success (no error)",
            libusb_error.LIBUSB_ERROR_IO => "Input/output error",
            libusb_error.LIBUSB_ERROR_INVALID_PARAM => "Invalid parameter",
            libusb_error.LIBUSB_ERROR_ACCESS => "Access denied (insufficient permissions)",
            libusb_error.LIBUSB_ERROR_NO_DEVICE => "No such device (it may have been disconnected)",
            libusb_error.LIBUSB_ERROR_NOT_FOUND => "Entity not found",
            libusb_error.LIBUSB_ERROR_BUSY => "Resource busy",
            libusb_error.LIBUSB_ERROR_TIMEOUT => "Operation timed out",
            libusb_error.LIBUSB_ERROR_OVERFLOW => "Overflow",
            libusb_error.LIBUSB_ERROR_PIPE => "Pipe error",
            libusb_error.LIBUSB_ERROR_INTERRUPTED => "System call interrupted (perhaps due to signal)",
            libusb_error.LIBUSB_ERROR_NO_MEM => "Insufficient memory",
            libusb_error.LIBUSB_ERROR_NOT_SUPPORTED => "Operation not supported or unimplemented on this platform",
            libusb_error.LIBUSB_ERROR_OTHER => "Other error",
            _ => $"Unknown error {error}",
        };

    public static Exception GetException(this libusb_error error, string? messagePrefix)
    {
        LibUsbException ex = new LibUsbException(messagePrefix, error);

        switch (error)
        {
            case libusb_error.LIBUSB_SUCCESS:
                return new InvalidOperationException("Tried to throw exception for libusb error that wasn't an errror.", ex);
            default:
                return ex;
        }
    }

    public static void Throw(this libusb_error error, string? messagePrefix = null)
        => throw new LibUsbException(messagePrefix, error);

    public static void ThrowIfError(this libusb_error error, string? messagePrefix = null)
    {
        if (error < libusb_error.LIBUSB_SUCCESS)
            error.Throw();
    }
}
