using System;
using System.Threading;
using static PicobootConnection.LibUsb.Globals;

namespace PicobootConnection.LibUsb;

public unsafe sealed class LibusbManager : IDisposable
{
    private readonly libusb_context _Context;
    private static LibusbManager? Instance;

    public static libusb_context Context => (Instance ??= new())._Context;

    private LibusbManager()
    {
        libusb_context context;
        libusb_init(&context).ThrowIfError("Error while initializing libusb");
        _Context = context;

        if (_Context.IsNull)
            throw new InvalidOperationException("Unknown error while initializing libusb.");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!_Context.IsNull)
            libusb_exit(_Context);

        Interlocked.CompareExchange(ref Instance, null, this);
    }

    public static void DisposeIfNeeded()
        => Instance?.Dispose();

    ~LibusbManager()
        => Dispose();
}
