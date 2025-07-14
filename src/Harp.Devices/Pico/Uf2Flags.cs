using System;

namespace Harp.Devices.Pico;

[Flags]
public enum Uf2Flags : uint
{
    None = 0,
    NotMainFlash = 0x00000001,
    FileContainer = 0x00001000,
    FamilyIdPresent = 0x00002000,
    Md5ChecksumPresent = 0x00004000,
    ExtensionTagsPresent = 0x00008000,
}
