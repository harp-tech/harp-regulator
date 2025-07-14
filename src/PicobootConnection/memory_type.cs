namespace PicobootConnection;

public enum memory_type
{
    rom,
    flash,
    sram,
    sram_unstriped,
    xip_sram,
    invalid,
}

public static class memory_typeEx
{
    public static string FriendlyName(this memory_type type)
        => type switch
        {
            memory_type.rom => "ROM",
            memory_type.flash => "flash",
            memory_type.sram => "SRAM",
            memory_type.sram_unstriped => "SRAM (unstriped)",
            memory_type.xip_sram => "XIP RAM",
            memory_type.invalid => "(invalid memory type)",
            _ => $"Unknown Memory Type {type}",
        };
}
