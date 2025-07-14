using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Harp.Protocol;

public class HarpMessage
{
    public readonly MessageType MessageType;
    public readonly byte Address;
    public readonly byte Port;
    public readonly PayloadType PayloadType;
    public readonly HarpTimestamp Timestamp;

    private readonly byte[] Payload;
    public ReadOnlySpan<byte> RawPayload => Payload;

    public readonly byte Checksum;
    public readonly byte CalculatedChecksum;

    public bool IsValid
        => MessageType.IsValid()
        && PayloadType.IsValid
        && Checksum == CalculatedChecksum;

    internal HarpMessage(HarpMessageParser parser)
    {
        MessageType = parser.MessageType;
        Address = parser.Address;
        Port = parser.Port;
        PayloadType = parser.PayloadType;
        Timestamp = parser.Timestamp;
        Payload = parser.Payload;
        Checksum = parser.Checksum;
        CalculatedChecksum = parser.CalculatedChecksum;
    }

    internal static HarpMessage Create(HarpMessageParser parser)
    {
        if (parser.ParserState != HarpMessageParser.State.Done)
            throw new ArgumentException("Parser has not parsed a full message yet!", nameof(parser));

        if (!parser.PayloadType.TryGetType(out Type? type))
            return new HarpMessage(parser);
        else if (type == typeof(Half))
            return new HarpMessage<Half>(parser);
        else if (type == typeof(float))
            return new HarpMessage<float>(parser);
        else if (type == typeof(double))
            return new HarpMessage<double>(parser);
        else if (type == typeof(sbyte))
            return new HarpMessage<sbyte>(parser);
        else if (type == typeof(short))
            return new HarpMessage<short>(parser);
        else if (type == typeof(int))
            return new HarpMessage<int>(parser);
        else if (type == typeof(long))
            return new HarpMessage<long>(parser);
        else if (type == typeof(byte))
            return new HarpMessage<byte>(parser);
        else if (type == typeof(ushort))
            return new HarpMessage<ushort>(parser);
        else if (type == typeof(uint))
            return new HarpMessage<uint>(parser);
        else if (type == typeof(ulong))
            return new HarpMessage<ulong>(parser);
        else
            throw new UnreachableException();
    }
}

public sealed class HarpMessage<T> : HarpMessage
    where T : unmanaged
{
    public ReadOnlySpan<T> Payload => MemoryMarshal.Cast<byte, T>(RawPayload);

    unsafe internal HarpMessage(HarpMessageParser parser)
        : base(parser)
    { }
}
