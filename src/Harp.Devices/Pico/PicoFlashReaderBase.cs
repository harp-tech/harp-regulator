using PicobootConnection;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Harp.Devices.Pico;

/// <remarks>This is roughly requivalent to `struct memory_access` in picotool.</remarks>
public abstract class PicoFlashReaderBase
{
    public abstract uint BinaryStart { get; }

    public abstract void Read(uint address, Span<byte> buffer);

    public void Read<T>(uint address, Span<T> values)
        where T : unmanaged
        => Read(address, MemoryMarshal.Cast<T, byte>(values));

    public T Read<T>(uint address)
        where T : unmanaged
    {
        T result;
        Unsafe.SkipInit(out result);
        Read(address, MemoryMarshal.CreateSpan(ref result, 1));
        return result;
    }

    public string ReadString(uint address)
    {
        // 512 is the maximum length picotool uses
        Span<byte> buffer = stackalloc byte[512];
        Read(address, buffer);
        return Encoding.UTF8.GetString(buffer.SliceNullTerminated(0));
    }

    public abstract model_t ReadModel();

    public override abstract string ToString();
}
