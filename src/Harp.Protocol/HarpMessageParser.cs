using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Harp.Protocol;

public ref struct HarpMessageParser
{
    public enum State
    {
        NeedMessageType,
        NeedLength,
        NeedAddress,
        NeedPort,
        NeedPayloadType,
        NeedTimestamp,
        NeedPayload,
        NeedChecksum,
        Done,
    }

    public State ParserState { get; private set; }

    internal MessageType MessageType;
    private ushort RemainingBytes;
    internal byte Address;
    internal byte Port;
    internal PayloadType PayloadType;
    internal HarpTimestamp Timestamp;
    private int PayloadWriteHead;
    internal byte[] Payload;
    internal byte Checksum;
    internal byte CalculatedChecksum;

    private unsafe T ConsumeField<T>(ref ReadOnlySpan<byte> bytes)
            where T : unmanaged
    {
        if (bytes.Length < sizeof(T))
            throw new InvalidOperationException();

        if (ParserState < State.NeedChecksum)
        {
            for (int i = 0; i < sizeof(T); i++)
                CalculatedChecksum += bytes[i];
        }

        if (ParserState > State.NeedLength)
        {
            Debug.Assert(RemainingBytes >= sizeof(T));
            RemainingBytes -= checked((ushort)sizeof(T));
        }

        T result = Unsafe.ReadUnaligned<T>(in bytes[0]);
        bytes = bytes.Slice(sizeof(T));
        return result;
    }

    private unsafe void HandleField<T>(ref ReadOnlySpan<byte> bytes, State requiredState, ref T value)
            where T : unmanaged
    {
        if (ParserState != requiredState || bytes.Length < sizeof(T))
            return;

        value = ConsumeField<T>(ref bytes);
        ParserState++;
    }

    public HarpMessage? Consume(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        if (ParserState == State.Done)
            throw new InvalidOperationException("This parser has completed.");

        int startingLength = buffer.Length;
        HandleField(ref buffer, State.NeedMessageType, ref MessageType);

        // Length is special due to extended length messages
        if (ParserState == State.NeedLength && buffer.Length >= 1)
        {
            byte smallLength = 0;
            if (buffer[0] != 255)
            {
                HandleField(ref buffer, State.NeedLength, ref smallLength);
                Debug.Assert(smallLength != 255);
                RemainingBytes = smallLength;
            }
            else if (buffer.Length >= 3)
            {
                HandleField(ref buffer, State.NeedLength, ref smallLength);
                Debug.Assert(smallLength == 255);
                RemainingBytes = ConsumeField<ushort>(ref buffer);
            }
        }

        HandleField(ref buffer, State.NeedAddress, ref Address);
        HandleField(ref buffer, State.NeedPort, ref Port);
        HandleField(ref buffer, State.NeedPayloadType, ref PayloadType);

        if (ParserState == State.NeedTimestamp)
        {
            if (PayloadType.HasTimestamp)
            { HandleField(ref buffer, State.NeedTimestamp, ref Timestamp); }
            else
            {
                Timestamp = HarpTimestamp.Invalid;
                ParserState++;
            }
        }

        if (ParserState == State.NeedPayload && buffer.Length > 0)
        {
            if (Payload is null)
            {
                PayloadWriteHead = 0;
                Payload = GC.AllocateUninitializedArray<byte>(RemainingBytes - 1); // -1 for checksum
            }

            Span<byte> payloadBuffer = Payload.AsSpan().Slice(PayloadWriteHead);
            int bytesToCopy = Math.Min(payloadBuffer.Length, buffer.Length);
            buffer.Slice(0, bytesToCopy).CopyTo(payloadBuffer);
            RemainingBytes -= checked((ushort)bytesToCopy);
            PayloadWriteHead += bytesToCopy;
            buffer = buffer.Slice(bytesToCopy);

            if (PayloadWriteHead >= Payload.Length)
            {
                ParserState++;

                foreach (byte data in Payload)
                    CalculatedChecksum += data;
            }
        }

        HandleField(ref buffer, State.NeedChecksum, ref Checksum);

        bytesConsumed = startingLength - buffer.Length;

        if (ParserState == State.Done)
        {
            Debug.Assert(RemainingBytes == 0);
            HarpMessage result = HarpMessage.Create(this);
            return result;
        }

        return null;
    }

    public void Reset()
        => this = default;
}
