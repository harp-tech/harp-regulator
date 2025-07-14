namespace PicobootConnection;

public enum model_t
{
    rp2040,
    rp2350,
    unknown
}

public static class model_tEx
{
    public static string FriendlyName(this model_t model)
        => model switch
        {
            model_t.rp2040 => "RP2040",
            model_t.rp2350 => "RP2350",
            model_t.unknown => "Unknown",
            _ => $"Unknown#{(int)model}",
        };
}
