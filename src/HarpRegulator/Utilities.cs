namespace HarpRegulator;

internal static class Utilities
{
    public static string FriendlyByteCount(uint byteCount, string format = "N")
    {
        if (byteCount == 1)
            return $"{byteCount.ToString(format)} byte";
        else if (byteCount < 1024)
            return $"{byteCount.ToString(format)} bytes";

        double value = byteCount / 1024.0;
        if (value < 1024.0)
            return $"{value.ToString(format)} KiB";

        value /= 1024.0;
        if (value < 1024.0)
            return $"{value.ToString(format)} MiB";

        value /= 1024.0;
        if (value < 1024.0)
            return $"{value.ToString(format)} GiB";

        value /= 1024.0;
        return $"{value.ToString(format)} TiB";
    }
}
