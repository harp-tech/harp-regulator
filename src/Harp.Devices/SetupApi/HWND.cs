using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Harp.Devices.SetupApi;

internal unsafe readonly partial struct HWND : IEquatable<HWND>
{
    private readonly void* Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HWND(void* value)
        => Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator void*(HWND handle)
        => handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator IntPtr(HWND handle)
        => (IntPtr)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator UIntPtr(HWND handle)
        => (UIntPtr)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HWND(void* handle)
        => new HWND(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HWND(IntPtr handle)
        => new HWND((void*)handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HWND(UIntPtr handle)
        => new HWND((void*)handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HWND(NullReference? handle)
        => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(HWND a, HWND b)
        => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(HWND a, HWND b)
        => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(HWND other)
        => this.Value == other.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj is HWND other ? Equals(other) : false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
        => ((IntPtr)Value).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
        => $"HWND(0x{((IntPtr)Value).ToString(Environment.Is64BitProcess ? "X16" : "X8")})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HWND ThrowIfNull()
    {
        if (Value == null)
        { throw new Win32Exception(); }

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HANDLE(HWND handle)
        => (HANDLE)(IntPtr)handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HWND(HANDLE handle)
        => (HWND)(IntPtr)handle;
}
