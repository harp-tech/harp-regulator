using System;
using System.Collections.Immutable;

namespace Harp.Devices;

internal static class Extensions
{
    public static ReadOnlySpan<byte> SliceNullTerminated(this ReadOnlySpan<byte> span, int start = 0)
    {
        span = span.Slice(start);
        int nullOffset = span.IndexOf((byte)0);
        return nullOffset == -1 ? span : span.Slice(0, nullOffset);
    }

    public static Span<byte> SliceNullTerminated(this Span<byte> span, int start = 0)
    {
        span = span.Slice(start);
        int nullOffset = span.IndexOf((byte)0);
        return nullOffset == -1 ? span : span.Slice(0, nullOffset);
    }

    public static ReadOnlySpan<char> SliceNullTerminated(this ReadOnlySpan<char> span, int start = 0)
    {
        span = span.Slice(start);
        int nullOffset = span.IndexOf('\0');
        return nullOffset == -1 ? span : span.Slice(0, nullOffset);
    }
}
