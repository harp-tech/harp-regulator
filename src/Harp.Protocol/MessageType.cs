namespace Harp.Protocol;

public enum MessageType : byte
{
    Read = 1,
    Write = 2,
    Event = 3,
    ReadError = 9,
    WriteError = 10,
}

public static class MessageTypeEx
{
    public static bool IsValid(this MessageType type)
        => type is MessageType.Read or MessageType.Write or MessageType.Event or MessageType.ReadError or MessageType.WriteError;
}
