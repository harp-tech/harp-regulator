using System;
using System.Diagnostics.CodeAnalysis;

namespace Harp.Devices.SetupApi;

internal struct DEVPROPTYPE : IEquatable<DEVPROPTYPE>
{
    private uint Value;

    public DEVPROPTYPE(DEVPROP_TYPE type, DEVPROP_TYPEMOD modifier = DEVPROP_TYPEMOD.None)
    {
        Value = 0;
        Type = type;
        Modifier = modifier;
    }

    private const uint DEVPROP_MASK_TYPE = 0x00000FFF;
    private const uint DEVPROP_MASK_TYPEMOD = 0x0000F000;

    public DEVPROP_TYPE Type
    {
        get => (DEVPROP_TYPE)(Value & DEVPROP_MASK_TYPE);
        set
        {
            uint uintValue = (uint)value;
            if ((uintValue & DEVPROP_MASK_TYPE) != uintValue)
                throw new ArgumentException("The specified type is invalid.", nameof(value));

            Value = (Value & ~DEVPROP_MASK_TYPE) | uintValue;
        }
    }

    public DEVPROP_TYPEMOD Modifier
    {
        get => (DEVPROP_TYPEMOD)(Value & DEVPROP_MASK_TYPEMOD);
        set
        {
            uint uintValue = (uint)value;
            if ((uintValue & DEVPROP_MASK_TYPEMOD) != uintValue)
                throw new ArgumentException("The specified type modifier is invalid.", nameof(value));

            Value = (Value & ~DEVPROP_MASK_TYPEMOD) | uintValue;
        }
    }

    public override string ToString()
        => Modifier == DEVPROP_TYPEMOD.None ? Type.ToString() : $"{Type} | {Modifier}";

    public bool Equals(DEVPROPTYPE other)
        => this.Value == other.Value;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is DEVPROPTYPE other ? Equals(other) : false;

    public static bool operator ==(DEVPROPTYPE a, DEVPROPTYPE b)
        => a.Equals(b);

    public static bool operator !=(DEVPROPTYPE a, DEVPROPTYPE b)
        => !a.Equals(b);

    public override int GetHashCode()
        => Value.GetHashCode();

    public static DEVPROPTYPE EMPTY => new(DEVPROP_TYPE.EMPTY);
    public static DEVPROPTYPE NULL => new(DEVPROP_TYPE.NULL);
    public static DEVPROPTYPE SBYTE => new(DEVPROP_TYPE.SBYTE);
    public static DEVPROPTYPE BYTE => new(DEVPROP_TYPE.BYTE);
    public static DEVPROPTYPE INT16 => new(DEVPROP_TYPE.INT16);
    public static DEVPROPTYPE UINT16 => new(DEVPROP_TYPE.UINT16);
    public static DEVPROPTYPE INT32 => new(DEVPROP_TYPE.INT32);
    public static DEVPROPTYPE UINT32 => new(DEVPROP_TYPE.UINT32);
    public static DEVPROPTYPE INT64 => new(DEVPROP_TYPE.INT64);
    public static DEVPROPTYPE UINT64 => new(DEVPROP_TYPE.UINT64);
    public static DEVPROPTYPE FLOAT => new(DEVPROP_TYPE.FLOAT);
    public static DEVPROPTYPE DOUBLE => new(DEVPROP_TYPE.DOUBLE);
    public static DEVPROPTYPE DECIMAL => new(DEVPROP_TYPE.DECIMAL);
    public static DEVPROPTYPE GUID => new(DEVPROP_TYPE.GUID);
    public static DEVPROPTYPE CURRENCY => new(DEVPROP_TYPE.CURRENCY);
    public static DEVPROPTYPE DATE => new(DEVPROP_TYPE.DATE);
    public static DEVPROPTYPE FILETIME => new(DEVPROP_TYPE.FILETIME);
    public static DEVPROPTYPE BOOLEAN => new(DEVPROP_TYPE.BOOLEAN);
    public static DEVPROPTYPE STRING => new(DEVPROP_TYPE.STRING);
    public static DEVPROPTYPE STRING_LIST => new(DEVPROP_TYPE.STRING, DEVPROP_TYPEMOD.LIST);
    public static DEVPROPTYPE SECURITY_DESCRIPTOR => new(DEVPROP_TYPE.SECURITY_DESCRIPTOR);
    public static DEVPROPTYPE SECURITY_DESCRIPTOR_STRING => new(DEVPROP_TYPE.SECURITY_DESCRIPTOR_STRING);
    public static DEVPROPTYPE DEVPROPKEY => new(DEVPROP_TYPE.DEVPROPKEY);
    public static DEVPROPTYPE _DEVPROPTYPE => new(DEVPROP_TYPE.DEVPROPTYPE);
    public static DEVPROPTYPE BINARY => new(DEVPROP_TYPE.BYTE, DEVPROP_TYPEMOD.ARRAY);
    public static DEVPROPTYPE ERROR => new(DEVPROP_TYPE.ERROR);
    public static DEVPROPTYPE NTSTATUS => new(DEVPROP_TYPE.NTSTATUS);
    public static DEVPROPTYPE STRING_INDIRECT => new(DEVPROP_TYPE.STRING_INDIRECT);

}
