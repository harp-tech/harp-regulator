using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Harp.Devices.SetupApi;

internal unsafe readonly partial struct HDEVINFO : IEquatable<HDEVINFO>
{
    private readonly void* Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HDEVINFO(void* value)
        => Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator void*(HDEVINFO handle)
        => handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator IntPtr(HDEVINFO handle)
        => (IntPtr)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator UIntPtr(HDEVINFO handle)
        => (UIntPtr)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HDEVINFO(void* handle)
        => new HDEVINFO(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HDEVINFO(IntPtr handle)
        => new HDEVINFO((void*)handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HDEVINFO(UIntPtr handle)
        => new HDEVINFO((void*)handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HDEVINFO(NullReference? handle)
        => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(HDEVINFO a, HDEVINFO b)
        => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(HDEVINFO a, HDEVINFO b)
        => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(HDEVINFO other)
        => this.Value == other.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj is HDEVINFO other ? Equals(other) : false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
        => ((IntPtr)Value).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
        => $"HDEVINFO(0x{((IntPtr)Value).ToString(Environment.Is64BitProcess ? "X16" : "X8")})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HDEVINFO ThrowIfNull()
    {
        if (Value == null)
        { throw new Win32Exception(); }

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HANDLE(HDEVINFO handle)
        => (HANDLE)(IntPtr)handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HDEVINFO(HANDLE handle)
        => (HDEVINFO)(IntPtr)handle;
}
