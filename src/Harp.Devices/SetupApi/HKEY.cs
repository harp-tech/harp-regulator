using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Harp.Devices.SetupApi;

internal unsafe readonly partial struct HKEY : IEquatable<HKEY>
{
    private readonly void* Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HKEY(void* value)
        => Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator void*(HKEY handle)
        => handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator IntPtr(HKEY handle)
        => (IntPtr)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator UIntPtr(HKEY handle)
        => (UIntPtr)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HKEY(void* handle)
        => new HKEY(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HKEY(IntPtr handle)
        => new HKEY((void*)handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HKEY(UIntPtr handle)
        => new HKEY((void*)handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HKEY(NullReference? handle)
        => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(HKEY a, HKEY b)
        => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(HKEY a, HKEY b)
        => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(HKEY other)
        => this.Value == other.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj is HKEY other ? Equals(other) : false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
        => ((IntPtr)Value).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
        => $"HKEY(0x{((IntPtr)Value).ToString(Environment.Is64BitProcess ? "X16" : "X8")})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HKEY ThrowIfNull()
    {
        if (Value == null)
        { throw new Win32Exception(); }

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HANDLE(HKEY handle)
        => (HANDLE)(IntPtr)handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator HKEY(HANDLE handle)
        => (HKEY)(IntPtr)handle;
}
