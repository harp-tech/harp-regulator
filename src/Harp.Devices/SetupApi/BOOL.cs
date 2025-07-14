using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

// Note that because this type defines implicit casts to bool, it does not need operator true, operator false, operator ==, etc.
[StructLayout(LayoutKind.Sequential)]
internal readonly struct BOOL : IComparable, IComparable<bool>, IEquatable<bool>, IComparable<BOOL>, IEquatable<BOOL>
{
    private readonly int Value;

    private BOOL(bool value)
        => Value = value ? 1 : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(BOOL b)
        => b.Value != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BOOL(bool b)
        => new BOOL(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
        => ((bool)this).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
        => ((bool)this).ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(IFormatProvider? provider)
        => ((bool)this).ToString(provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten)
        => ((bool)this).TryFormat(destination, out charsWritten);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj switch
        {
            bool boolean => this == boolean,
            BOOL nativeBool => this == nativeBool,
            _ => false
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(bool other)
        => this == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(BOOL other)
        => this == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(object? obj)
        => ((bool)this).CompareTo(obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(bool value)
        => ((bool)this).CompareTo(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(BOOL value)
        => CompareTo((bool)this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BOOL ThrowIfFalse()
        => this ? this : throw new Win32Exception();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BOOL ThrowIfTrue()
        => this ? throw new Win32Exception() : this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BOOL AssertTrue(string? messagePrefix = null)
    {
        AssertValue(messagePrefix, true);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BOOL AssertFalse(string? messagePrefix = null)
    {
        AssertValue(messagePrefix, false);
        return this;
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertValue(string? messagePrefix, bool expectedValue)
    {
        if (this != expectedValue)
        {
            Win32Error error = Windows.GetLastError();
            messagePrefix ??= $"Expected {expectedValue} result";
            Debug.Fail($"{messagePrefix}: {error.GetMessage()}");
        }
    }
}
