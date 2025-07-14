using PicobootConnection;
using PicobootConnection.LibUsb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PicobootConnection.LibUsb.Globals;
using static PicobootConnection.Picoboot;

namespace Harp.Devices.Pico;

public sealed partial class PicobootDevice : IDisposable
{
    private static HashSet<GCHandle> AllPicobootDevices = new();
    private readonly GCHandle ThisGCHandle;

    private string Identity { get; }
    public libusb_device_handle Handle { get; }
    public model_t Model { get; }

    private readonly bool Exclusive;

    private bool HaveUniqueId = false;
    private ulong? _UniqueId;

    private PicobootDevice(string identity, model_t model, libusb_device_handle handle, bool exclusive = true)
    {
        AllPicobootDevices.Add(ThisGCHandle = GCHandle.Alloc(this, GCHandleType.Weak));
        Identity = identity;
        Model = model;
        Exclusive = exclusive;

        if (exclusive)
        {
            int status = picoboot_exclusive_access(handle, picoboot_exclusive_type.EXCLUSIVE);
            HandleReturnCode("Exclusive access lock command failed", status);
        }

        // Assigned last so we only dispose of it if ownership is fully transferred
        Handle = handle;
    }

    /// <summary>For the RP2040, this is the flash ID. For the RP2350, this is the chip's embedded uniqued ID.</summary>
    public ulong? UniqueId
    {
        get
        {
            if (!HaveUniqueId)
            {
                _UniqueId = GetUniqueId();
                HaveUniqueId = true;
            }

            return _UniqueId;

            // Same logic as info_guts in picotool
            [MethodImpl(MethodImplOptions.NoInlining)]
            unsafe ulong? GetUniqueId()
            {
                int result;

                if (Model == model_t.rp2040)
                {
                    ulong flashId;
                    //TODO: Make sure this fails gracefully on systems without a flash chip
                    result = picoboot_flash_id(Handle, &flashId);

                    HandleReturnCode("Get flash ID command failed", result, justWriteTraceMessage: true);
                    return flashId;
                }
                else if (Model == model_t.rp2350)
                {
                    Span<uint> info = stackalloc uint[64];
                    picoboot_get_info_cmd command = new()
                    {
                        bType = PicobootInfoType.PICOBOOT_GET_INFO_SYS,
                        dParams0 = (uint)PicoSysInfoFlags.SYS_INFO_CHIP_INFO,
                    };
                    fixed (uint* infoP = info)
                    {
                        int lengthBytes = info.Length * sizeof(uint);
                        result = Rp2350.picoboot_get_info(Handle, &command, (byte*)infoP, checked((uint)lengthBytes));
                    }

                    HandleReturnCode("Get info command failed", result, justWriteTraceMessage: true);

                    Span<uint> data = info;
                    uint wordCount = data[0];
                    data = data.Slice(1, checked((int)wordCount));

                    PicoSysInfoFlags included = (PicoSysInfoFlags)data[0];
                    data = data.Slice(1);

                    if (included.HasFlag(PicoSysInfoFlags.SYS_INFO_CHIP_INFO))
                        return data[1] | ((ulong)data[2] << 32);

                    Trace.WriteLine($"{Model} {Identity} didn't respond with the requested chip info.");
                    return null;
                }
                else
                {
                    Trace.WriteLine($"Not sure how to get a unique ID of unknown Pico model {Model} {Identity}");
                    return null;
                }
            }
        }
    }

    public void ExitXip()
    {
        int status = Picoboot.picoboot_exit_xip(Handle);
        HandleReturnCode("Exit XIP", status);
    }

    /// <remarks>Based on picotool's picoboot_memory_access::read_raw</remarks>
    public unsafe void ReadAligned(uint baseAddress, Span<byte> buffer)
    {
        uint length = checked((uint)buffer.Length);
        uint endAddress = baseAddress + length;
        memory_type type = PBC_get_memory_type(baseAddress, Model);
        memory_type endType = PBC_get_memory_type(endAddress, Model);

        if (type != endType)
            throw new InvalidOperationException("The write operation must not span multiple memory regions.");

        if (type == memory_type.flash)
        {
            if (baseAddress % PAGE_SIZE != 0)
                throw new ArgumentException("The start of the write operation must lie on a flash page boundary.", nameof(baseAddress));

            if (endAddress % PAGE_SIZE != 0)
                throw new ArgumentException("The end of the write operation must lie on a flash page boundary.", nameof(buffer));
        }

        if (type == memory_type.flash)
            ExitXip();

        // Reading from the ROM past this address requires special handling that isn't implemented here
        if (type == memory_type.rom && endAddress >= 0x2000)
            throw new NotSupportedException();

        int status;
        fixed (byte* dataP = buffer)
            status = picoboot_read(Handle, baseAddress, dataP, length);
        HandleReturnCode("Read command failed", status);
    }

    public void Read(uint baseAddress, Span<byte> buffer)
    {
        memory_type type = PBC_get_memory_type(baseAddress, Model);
        AddressRange range = new(baseAddress, baseAddress + checked((uint)buffer.Length));

        if (type != memory_type.flash || range.IsAligned(PAGE_SIZE))
        {
            ReadAligned(baseAddress, buffer);
        }
        else
        {
            range = range.GetAligned(PAGE_SIZE);
            Debug.Assert(range.Size < int.MaxValue);
            Span<byte> tempBuffer = range.Size <= 4096 ? stackalloc byte[(int)range.Size] : new byte[(int)range.Size];
            ReadAligned(range.Start, tempBuffer);
            int offset = checked((int)(baseAddress - range.Start));
            tempBuffer.Slice(offset, buffer.Length).CopyTo(buffer);
        }
    }

    public void FlashErase(AddressRange range)
    {
        if (PBC_get_memory_type(range.Start, Model) != memory_type.flash || PBC_get_memory_type(range.End, Model) != memory_type.flash)
            throw new ArgumentException("The specified memory range does not lie fully within the flash.", nameof(range));
        if (range.Start % FLASH_SECTOR_ERASE_SIZE != 0 || range.End % FLASH_SECTOR_ERASE_SIZE != 0)
            throw new ArgumentException("The specified memory range is not aligned to the flash sector erase size.", nameof(range));

        int status = Picoboot.picoboot_flash_erase(Handle, range.Start, range.Size);
        HandleReturnCode("Flash erase failed", status);
    }

    public unsafe void Write(uint baseAddress, ReadOnlySpan<byte> data)
    {
        uint length = checked((uint)data.Length);
        uint endAddress = baseAddress + length;
        memory_type type = PBC_get_memory_type(baseAddress, Model);
        memory_type endType = PBC_get_memory_type(endAddress, Model);

        if (type != endType)
            throw new InvalidOperationException("The write operation must not span multiple memory regions.");

        if (type == memory_type.flash)
        {
            if (baseAddress % PAGE_SIZE != 0)
                throw new ArgumentException("The start of the write operation must lie on a flash page boundary.", nameof(baseAddress));

            if (endAddress % PAGE_SIZE != 0)
                throw new ArgumentException("The end of the write operation must lie on a flash page boundary.", nameof(data));
        }

        int status;
        fixed (byte* dataP = data)
            status = picoboot_write(Handle, baseAddress, dataP, length);
        HandleReturnCode("Write command failed", status);
    }

    /// <remarks>Based on logic in picotool's <c>load_guts</c>.</remarks>
    public unsafe void Reboot(uint binaryStart)
    {
        memory_type memoryType = PBC_get_memory_type(binaryStart, Model);
        const uint delayMs = 500;

        if (Model == model_t.rp2350)
        {
            picoboot_reboot2_cmd command;
            if (binaryStart == 0)
            {
                command = new()
                {
                    dFlags = PicobootReboot2Flags.REBOOT2_FLAG_REBOOT_TYPE_NORMAL,
                    dParam0 = 0,
                };
            }
            else if (memoryType == memory_type.flash)
            {
                command = new()
                {
                    dFlags = PicobootReboot2Flags.REBOOT2_FLAG_REBOOT_TYPE_FLASH_UPDATE,
                    dParam0 = 0,
                };
            }
            else if (memoryType is memory_type.sram or memory_type.xip_sram)
            {
                command = new()
                {
                    dFlags = PicobootReboot2Flags.REBOOT2_FLAG_REBOOT_TYPE_RAM_IMAGE,
                    dParam0 = binaryStart,
                    dParam1 = memoryType switch
                    {
                        memory_type.sram => PicoMemoryMap.SRAM_END_RP2350,
                        memory_type.xip_sram => PicoMemoryMap.XIP_SRAM_END_RP2350,
                        _ => throw new UnreachableException(),
                    },
                };
            }
            else
            { throw new ArgumentOutOfRangeException(nameof(binaryStart), $"The binary start must be 0, a flash address, an SRAM address, or an XIP SRAM address."); }

            command.dDelayMS = delayMs;
            int status = Rp2350.picoboot_reboot2(Handle, &command);
            HandleReturnCode("RP2350 reboot command failed", status);
        }
        else if (Model == model_t.rp2040)
        {
            uint end;
            switch (memoryType)
            {
                case memory_type.flash:
                    binaryStart = 0;
                    end = 0;
                    break;
                case memory_type.sram:
                    end = PicoMemoryMap.SRAM_END_RP2040;
                    break;
                case memory_type.xip_sram:
                    // Picotool doesn't properly handle this case
                    end = PicoMemoryMap.XIP_SRAM_END_RP2040;
                    break;
                default:
                    if (binaryStart != 0)
                        throw new ArgumentOutOfRangeException(nameof(binaryStart), $"The binary start must be 0, a flash address, an SRAM address, or an XIP SRAM address.");
                    end = 0;
                    break;
            }

            int status = picoboot_reboot(Handle, binaryStart, end, delayMs);
            HandleReturnCode("RP2040 reboot command failed", status);
        }
        else
        { throw new InvalidOperationException($"Unsure how to reboot {Model} device."); }
    }

    public void Reboot()
        => Reboot(0);

    public void Reboot(Uf2View firmware, bool ignoreNonBootable = false)
    {
        uint binaryStart = PicoMemoryMap.FindBinaryStart(firmware);
        if (binaryStart == 0 && !ignoreNonBootable)
            throw new ArgumentException("The specified firmware does not contain a valid RP2 executable image", nameof(firmware));

        Reboot(binaryStart);
    }

    private uint? _PreviouslyGuessedFlashSize = null;
    /// <returns>The size of the device's flash storage, or 0 if the flash storage is not present or has never had anything written.</returns>
    /// <remarks>Based on Picotool's guess_flash_size</remarks>
    public uint TryGetFlashSize()
    {
        if (_PreviouslyGuessedFlashSize is uint cachedValue)
            return cachedValue;

        Span<byte> firstTwoPages = stackalloc byte[(int)PAGE_SIZE * 2];
        ReadAligned(PicoMemoryMap.FLASH_START, firstTwoPages);

        ReadOnlySpan<byte> page0 = firstTwoPages.Slice(0, (int)PAGE_SIZE);
        ReadOnlySpan<byte> page1 = firstTwoPages.Slice((int)PAGE_SIZE);

        if (page0.SequenceEqual(page1))
        {
            Trace.WriteLine("Could not guess device's flash size: The first two pages are identical.");
            Trace.WriteLine("(Flash is either not present or has never been written.)");
            return 0;
        }

        // Read at decreasing power-of-two addresses until we don't see the mirror of the boot pages again
        const uint minSize = 16 * (int)PAGE_SIZE;
        const uint maxSize = 8 * 1024 * 1024;
        Debug.Assert(minSize >= FLASH_SECTOR_ERASE_SIZE);
        Debug.Assert(BitOperations.IsPow2(minSize));
        Debug.Assert(BitOperations.IsPow2(maxSize));
        uint size;
        Span<byte> newPages = stackalloc byte[(int)PAGE_SIZE * 2];
        for (size = maxSize; size >= minSize; size /= 2)
        {
            ReadAligned(PicoMemoryMap.FLASH_START + size, newPages);
            if (!newPages.SequenceEqual(firstTwoPages))
                break;
        }

        size *= 2;
        Debug.Assert(size >= minSize && size <= maxSize);
        _PreviouslyGuessedFlashSize = size;
        return size;
    }

    /// <returns>The usable range of the device's flash storage, or a 0-sized range if the flash storage is not present or has never had anything written.</returns>
    /// <remarks>Based on Picotool's guess_flash_size</remarks>
    public AddressRange TryGetFlashRange()
        => new AddressRange(PicoMemoryMap.FLASH_START, PicoMemoryMap.FLASH_START + TryGetFlashSize());

    /// <remarks>Based on the logic in picotool's connection::wrap_call</remarks>
    private unsafe Exception? MakeExceptionForReturnCode(string? messagePrefix, int returnCode)
    {
        if (returnCode == 0)
            return null;

        //TODO: picoboot_connection seems to have a pattern of returning positive values for picoboot errors and negative values for libusb, we could
        // potentially take advantage of that to skip this check.
        picoboot_cmd_status status = new();
        int statusReturnCode = picoboot_cmd_status(Handle, &status);
        int resetStatus = 0; // Just so it's accessible via the debugger

        if (statusReturnCode == 0)
        {
            resetStatus = picoboot_reset(Handle);
            messagePrefix ??= "Picoboot command resulted in failure";
            return new PicobootCommandFailureException(messagePrefix, status.dStatusCode == picoboot_status.PICOBOOT_OK ? picoboot_status.PICOBOOT_UNKNOWN_ERROR : status.dStatusCode);
        }
        else
        {
            // Unlike wrap_call, we throw for the original failure reason so that it isn't hidden
            messagePrefix ??= "Picoboot command resulted in libusb failure";
            return new LibUsbException(messagePrefix, (libusb_error)returnCode);
        }
    }

    /// <remarks>Based on the logic in picotool's connection::wrap_call</remarks>
    private void HandleReturnCode(string? messagePrefix, int returnCode, bool justWriteTraceMessage = false)
    {
        if (MakeExceptionForReturnCode(messagePrefix, returnCode) is Exception ex)
        {
            if (justWriteTraceMessage)
                Trace.WriteLine(ex.Message);
            else
                throw ex;
        }
    }

    private void HandleReturnCode(int returnCode, bool justWriteTraceMessage = false)
        => HandleReturnCode(null, returnCode, justWriteTraceMessage);

    public override string ToString()
        => $"Picoboot device for {Identity}";

    public void Dispose()
        => Dispose(isDisposeAll: false);

    public void Dispose(bool isDisposeAll)
    {
        GC.SuppressFinalize(this);

        if (isDisposeAll)
        { Debug.Assert(AllPicobootDevices.Contains(ThisGCHandle)); }
        else
        {
            bool removed = AllPicobootDevices.Remove(ThisGCHandle);
            Debug.Assert(removed);
        }

        if (!Handle.IsNull)
        {
            if (Exclusive)
            {
                int status = picoboot_exclusive_access(Handle, picoboot_exclusive_type.NOT_EXCLUSIVE);
                if (status != 0)
                {
                    // Failed to restore exclusive access, just reset the device
                    status = picoboot_reset(Handle);
                    HandleReturnCode("Reset command failed", status);
                }
            }

            libusb_close(Handle);
        }
    }

    ~PicobootDevice()
        => Dispose();

    // This is a bit of a bodge. Device sort-of owns a PicobootDevice, but it'd be annoying for it to implement IDisposable since it's a record and
    // individual instances don't have a firm lifetime. It'd probably make more sense to provide a helper to do DisposeAll on an ImmutableArray<Device>,
    // but this gets the job done.
    public static void DisposeAll()
    {
        foreach (GCHandle handle in AllPicobootDevices)
        {
            PicobootDevice? device = (PicobootDevice?)handle.Target;
            device?.Dispose(isDisposeAll: true);
        }

        AllPicobootDevices.Clear();
    }
}
