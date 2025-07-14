//#define PRINT_BINARY_INFO
using PicobootConnection;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Harp.Devices.Pico;

//TODO: It would be nice if this could be replaced with or become an abstraction of a generic RP2 binary info reader
// (as opposed to a utility that's hard-coded to extract only a few bits of firmware metadata.)
public readonly struct PicoFirmwareInfo
{
    [JsonIgnore] public bool HaveInfo { get; }
    public string? ProgramName { get; }
    public string? Description { get; }
    public string? Version { get; }

    private PicoFirmwareInfo(PicoFlashReaderBase reader)
    {
        // Look for the binary info marker
        uint startRead = reader.BinaryStart;
        int maxSearch = 256;

        if (startRead == 0)
        {
            Trace.WriteLine($"Could not determine the start of the binary provided by {reader}.");
            return;
        }

        if (reader.ReadModel() == model_t.rp2040)
        {
            maxSearch = 64;

            // Skip boot2
            if (startRead == PicoMemoryMap.FLASH_START)
                startRead += 0x100;
        }

        Span<uint> startWords = stackalloc uint[maxSearch];
        reader.Read(startRead, startWords);

        const uint BINARY_INFO_MARKER_START = 0x7188ebf2;
        const uint BINARY_INFO_MARKER_END = 0xe71aa390;
        int binaryInfoTableOffset = 0;
        bool found = false;
        foreach (uint word in startWords)
        {
            if (word == BINARY_INFO_MARKER_START)
            {
                found = true;
                break;
            }

            binaryInfoTableOffset++;
        }

        if (!found)
            return;

        if (binaryInfoTableOffset + 4 < maxSearch && startWords[binaryInfoTableOffset + 4] != BINARY_INFO_MARKER_END)
            throw new InvalidOperationException("The binary info table is malformed.");

        uint binaryInfoStart = startWords[binaryInfoTableOffset + 1];
        uint binaryInfoEnd = startWords[binaryInfoTableOffset + 2];
#if PRINT_BINARY_INFO
        Console.WriteLine($"__binary_info_start = 0x{startWords[binaryInfoTableOffset + 1]:X8}");
        Console.WriteLine($"__binary_info_end = 0x{startWords[binaryInfoTableOffset + 2]:X8}");
        Console.WriteLine($"__address_mapping_table = 0x{startWords[binaryInfoTableOffset + 3]:X8}");
#endif

        // Parse the table
        Span<uint> binaryInfoTable = stackalloc uint[checked((int)(binaryInfoEnd - binaryInfoStart) / sizeof(uint))];
        reader.Read(binaryInfoStart, binaryInfoTable);
        int index = 0;
        foreach (uint binaryInfoPointer in binaryInfoTable)
        {
            //TODO: In theory using a union here might not be safe if the actual binary info was shorter than the longest one
            // Currently BinaryInfoUnion's actual members are all the same length, so it's not a problem, but it could be one if we expand this.
            BinaryInfoUnion info = reader.Read<BinaryInfoUnion>(binaryInfoPointer);
#if PRINT_BINARY_INFO
            Console.Write($"bi[{index}] = {info.ToString()}");
            if (info.Core.Type == BinaryInfoType.ID_AND_STRING)
                Console.Write($" = '{reader.ReadString(info.IdAndString.ValueAddress)}'");
            Console.WriteLine();
#endif

            if (info.Core.Type == BinaryInfoType.ID_AND_STRING)
            {
                string value = reader.ReadString(info.IdAndString.ValueAddress);
                switch (info.IdAndString.Id)
                {
                    case BinaryInfoId.RP_PROGRAM_NAME:
                        ProgramName = value;
                        HaveInfo = true;
                        break;
                    case BinaryInfoId.RP_PROGRAM_DESCRIPTION:
                        Description = value;
                        HaveInfo = true;
                        break;
                    case BinaryInfoId.RP_PROGRAM_VERSION_STRING:
                        Version = value;
                        HaveInfo = true;
                        break;
                }
            }

            index++;
        }
    }

    public static PicoFirmwareInfo GetInfo(Uf2View view)
        => new PicoFirmwareInfo(new Uf2FlashReader(view));

    public static PicoFirmwareInfo GetInfo(PicobootDevice device)
        => new PicoFirmwareInfo(new PhysicalDeviceFlashReader(device));
}
