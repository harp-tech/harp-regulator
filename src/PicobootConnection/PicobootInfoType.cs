namespace PicobootConnection;

public enum PicobootInfoType : byte
{
    PICOBOOT_GET_INFO_SYS = 1,
    PICOBOOT_GET_INFO_PARTTION_TABLE = 2,
    PICOBOOT_GET_INFO_UF2_TARGET_PARTITION = 3,
    PICOBOOT_GET_INFO_UF2_STATUS = 4,
}
