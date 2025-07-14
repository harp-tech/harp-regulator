﻿using Harp.Devices;
using Harp.Devices.Pico;
using PicobootConnection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace HarpRegulator;

internal sealed partial class UploadFirmwareCommand : CommandBase
{
    public override string Verb => "upload";
    public override string Description => "Uploads firmware to a specific device.";
    public override string? UsageHelp => "upload <firmware-file-path> --target <device> [--[no-]interactive] [--allow-connect|--no-connect] [--[no-]progress] [--no-reboot] [--no-upload] [--force]";

    public override string? ArgumentsHelp =>
        $"""
        <firmware-file-path>
            Path to a firmware blob in UF2 format to upload.

        --target <device>
            Targets a particular Harp device.
            <device> can be one of the following:
                {(OperatingSystem.IsWindows() ? "A COM port (EG: \"COM3\"" : "A path to a serial port TTY device (EG: \"/dev/ttyUSB0\")")}
                A device serial number in hex. Partial serial numbers accepted using prefix or suffix match.
                "PICOBOOT" - The first available PICOBOOT device (IE: an Pico-based Harp device already in BOOTSEL mode.)

        --interactive
        --no-interactive
            Whether or not to prompt the user to make decisions.
            (Default is enabled if console input is not redirected.)

        --allow-connect
        --no-connect
            Whether or not to allow connection to devices via the Harp protocol when searching for the target device.
            Connection will only be peformed if necessary, connecting to high confidence before low confidence, etc.
            (Default is to prompt the user if running interactively, disabled otherwise.)
            (Note that the target device may be connected to irrespective of this switch.)

        --progress
        --no-progress
            Show or hide firmware upload progress bars.
            (Default is to show progress when running interactively.)

        --no-reboot
            Do not reboot into the provided firmware once upload is complete.

        --no-upload
            Do not actually upload the firmware to the device.

        --force
            Whether to force firmware upload even if things seem incorrect.
            (IE: WhoAmI mismatch, attempting to flash device which doesn't appear to be a Harp device.)
        """;

    public override CommandResult Execute(Queue<string> arguments)
    {
        // Parse command line arguments
        bool interactive = !Console.IsInputRedirected;
        bool showProgress = interactive;
        bool force = false;
        bool doFirmwareUpload = true;
        bool rebootAfterUpload = true;
        string? targetFilter = null;
        string? firmwareFilePath = null;
        bool? allowHarpConnection = null;

        while (arguments.Count > 0)
        {
            string argument = arguments.Dequeue();
            switch (argument.ToLowerInvariant())
            {
                case "--target":
                    if (!arguments.TryDequeue(out targetFilter))
                    {
                        Console.Error.WriteLine($"A device filter must be specified for `--target`");
                        return CommandResult.Failure;
                    }
                    break;
                case "--interactive":
                    interactive = true;
                    break;
                case "--no-interactive":
                    interactive = false;
                    break;
                case "--allow-connect":
                    allowHarpConnection = true;
                    break;
                case "--no-connect":
                    allowHarpConnection = false;
                    break;
                case "--progress":
                    showProgress = true;
                    break;
                case "--no-progress":
                    showProgress = false;
                    break;
                case "--no-reboot":
                    rebootAfterUpload = false;
                    break;
                case "--no-upload":
                    doFirmwareUpload = false;
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                {
                    switch (TryHandleCommonArgument(argument))
                    {
                        case CommonArgumentResult.Handled:
                            break;
                        case CommonArgumentResult.ShowHelp:
                            return CommandResult.ShowHelp;
                        default:
                            if (firmwareFilePath is null)
                            {
                                firmwareFilePath = argument;
                                break;
                            }
                            else
                            {
                                Console.Error.WriteLine($"Unknown argument '{argument}'");
                                return CommandResult.Failure;
                            }
                    }
                    break;
                }
            }
        }

        if (firmwareFilePath is null)
        {
            Console.Error.WriteLine("A firmware file must be spcified.");
            return CommandResult.ShowHelp;
        }

        if (targetFilter is null)
        {
            Console.Error.WriteLine("A target device must be spcified.");
            return CommandResult.ShowHelp;
        }

        // Don't prompt for Harp connections if we aren't running interactively
        if (!interactive && allowHarpConnection is null)
            allowHarpConnection = false;

        // Load the UF2
        Uf2File file = new(firmwareFilePath);

        // Find target device
        ImmutableArray<Device> allDevices = Device.EnumerateDevices(allowConnection: null);
        Device? device = FindTargetDevice(allDevices, connectionLevel: null);
        if (device is null)
            return CommandResult.Failure;

        Console.WriteLine("Found target device!");
        ListDevicesCommand.ListDevices([device]);
        Console.WriteLine();

        if (device.Kind != DeviceKind.Pico)
        {
            Console.Error.WriteLine("Harp Regulator currently only supports Pico devices.");
            return CommandResult.Failure;
        }

        // Find the appropriate UF2 view for the device
        Uf2View? view = null;
        {
            bool ambiguousFamily = false;
            foreach (Uf2FamilyId family in file.FamilyIds)
            {
                //TODO: It would be more correct to ensure that the model matches the target device
                if (family.ToPicoModel() != model_t.unknown)
                {
                    if (view is not null)
                    {
                        if (!ambiguousFamily)
                        {
                            Console.Error.WriteLine($"'{firmwareFilePath}' contains multiple family IDs which could be applicable to this device:");
                            Console.Error.WriteLine($"* {view.FamilyId.Description()}");
                            ambiguousFamily = true;
                        }

                        Console.Error.WriteLine($"* {family.Description()}");
                    }

                    view = new(file, family);
                }
            }

            if (view is null)
            {
                Console.Error.WriteLine("The UF2 file doesn't contain firmware applicable to the device.");
                return CommandResult.Failure;
            }

            if (ambiguousFamily)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Could not determine which family to use. UF2 is malformed.");
                return CommandResult.Failure;
            }

            // Validate the UF2 view
            // This is roughly requivalent to the validation logic found in picotool's load_guts function
            {
                model_t model = view.FamilyId.ToPicoModel();
                foreach (ref readonly Uf2Block block in view)
                {
                    memory_type type1 = Picoboot.PBC_get_memory_type(block.AddressRange.Start, model);
                    memory_type type2 = Picoboot.PBC_get_memory_type(block.AddressRange.End, model);

                    if (type1 != type2 || type1 is memory_type.invalid or memory_type.rom or memory_type.sram_unstriped)
                    {
                        Console.Error.WriteLine($"{view} has contains data for {block.AddressRange}, which is not valid. (Memory type = {type1}{(type1 != type2 ? $"..{type2}" : "")})");
                        return CommandResult.Failure;
                    }
                }
            }
        }

        // Reboot the device if necessary
        bool deviceStartedOnline = false;
        if (device.State is DeviceState.Online or DeviceState.Unknown)
        {
            deviceStartedOnline = true;
            if (!SwitchToBootloader(ref allDevices, ref device, interactive, force))
                return CommandResult.Failure;
        }

        // Fail if the device cannot be communicated with
        if (device.State == DeviceState.DriverError)
        {
            Console.Error.WriteLine("Cannot communicate with the device, driver is in an erroneous state.");
            Console.Error.WriteLine("    Try running `HarpRegulator install-drivers` as admin.");
            return CommandResult.Failure;
        }

        // Fail if there's not Picoboot interface
        if (device.PicobootDevice is null)
        {
            Console.Error.WriteLine("Cannot communicate with the device, the PICOBOOT interface was not instantiated.");
            Console.Error.WriteLine("    --verbose may provide more details.");
            return CommandResult.Failure;
        }

        // Check if the firmware is applicable to this device
        if (!VerifyFirmwareCompatibility(device, view, interactive, force))
        {
            // Reboot the device to put it back online if applicable
            if (deviceStartedOnline)
            {
                Trace.WriteLine("We put the device in BOOTSEL mode, so we'll put it back in normal mode.");
                device.PicobootDevice.Reboot();
            }

            return CommandResult.Failure;
        }

        // Upload firmware
        if (doFirmwareUpload)
            UploadFirmware(view, device.PicobootDevice);
        else
            Console.WriteLine("Firmware upload skipped!");

        // Reboot into firmware
        if (rebootAfterUpload)
        {
            Console.WriteLine("Rebooting device...");
            device.PicobootDevice.Reboot(view, ignoreNonBootable: true);
        }

        device.PicobootDevice.Dispose();
        Console.WriteLine($"Finished uploading '{firmwareFilePath}' to device matching '{targetFilter}'");
        return CommandResult.Success;

        Device? FindTargetDevice(ImmutableArray<Device> allDevices, DeviceConfidence? connectionLevel)
        {
            ImmutableArray<Device> filteredDevices = allDevices.Filter(targetFilter);

            filteredDevices = allDevices.Filter(targetFilter);
            switch (filteredDevices.Length)
            {
                case > 1:
                    Console.Error.WriteLine($"Target filter '{targetFilter}' is ambiguous and matches multiple devices, listed below.");
                    ListDevicesCommand.ListDevices(filteredDevices, output: Console.Error);
                    Console.Error.WriteLine();
                    return null;
                case 1:
                    return filteredDevices[0];
                case 0:
                {
                    if (allDevices.Length == 0)
                    {
                        Console.Error.WriteLine("Nothing connected to this system looks like it could be a Harp device.");
                        return null;
                    }

                    // Connecting via Harp only does something if the devices have a serial port ports and are either Online or Unknown
                    bool couldConnectingActuallyHelp = allDevices.Any(d => d.State is DeviceState.Online or DeviceState.Unknown && d.PortName is not null);

                    ImmutableArray<Device>.Builder builder = allDevices.ToBuilder();
                    int numDevicesUpdated = 0;
                    bool showedPromptThisCall = false;
                    do
                    {
                        if (connectionLevel == DeviceConfidence.Zero || allowHarpConnection == false || !couldConnectingActuallyHelp)
                        {
                            if (showedPromptThisCall)
                            {
                                Console.Error.WriteLine("Failed to discover any more details from any of the above devices.");
                                if (allDevices.All(d => d.Confidence == DeviceConfidence.Zero))
                                    Console.Error.WriteLine("(Are any of these actually Harp devices?)");
                                return null;
                            }

                            Console.Error.WriteLine($"None of the following devices matched the filter '{targetFilter}':");
                            ListDevicesCommand.ListDevices(builder.ToImmutable(), output: Console.Error);
                            Console.Error.WriteLine();
                            return null;
                        }

                        if (connectionLevel is null && allowHarpConnection is null)
                        {
                            Debug.Assert(interactive);
                            Console.WriteLine($"Failed to find any devices matching the target filter '{targetFilter}' out of the following:");
                            ListDevicesCommand.ListDevices(builder.ToImmutable());
                            Console.WriteLine();
                            showedPromptThisCall = true;
                            if (!YesNo($"Do you want to try connecting to devices to attempt to find the target?", defaultChoice: false))
                            {
                                Console.Error.WriteLine($"No target device! Aborting.");
                                return null;
                            }
                        }

                        connectionLevel = connectionLevel is null ? DeviceConfidence.High : connectionLevel - 1;
                        Trace.WriteLine($"Didn't find any devices, trying to connect to devices with {connectionLevel} Confidence for more information...");
                        for (int i = 0; i < builder.Count; i++)
                        {
                            Device device = builder[i];
                            if (device.PortName is not null && device.Confidence == connectionLevel)
                            {
                                Device newDevice = device.WithMetadataFromHarpProtocol();
                                if (device == newDevice)
                                    continue;

                                builder[i] = newDevice;
                                numDevicesUpdated++;
                            }
                        }
                        Trace.WriteLine($"{numDevicesUpdated} device(s) were updated.");
                    } while (numDevicesUpdated == 0);

                    return FindTargetDevice(builder.ToImmutable(), connectionLevel);
                }
                default:
                    throw new UnreachableException();
            }
        }

        // The logic here is based on (but not identical to) picotool
        // https://github.com/raspberrypi/picotool/blob/de8ae5ac334e1126993f72a5c67949712fd1e1a4/main.cpp#L4591
        void UploadFirmware(Uf2View view, PicobootDevice device)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            Console.WriteLine("Uploading firmware...");
            Uf2FlashReader uf2Reader = new(view);

            // This can be any multiple of FLASH_SECTOR_ERASE_SIZE
            Span<byte> _buffer = stackalloc byte[(int)Picoboot.FLASH_SECTOR_ERASE_SIZE];
            Debug.Assert(_buffer.Length % Picoboot.FLASH_SECTOR_ERASE_SIZE == 0);

            foreach (AddressRange coalescedRange in view.CoalescedRanges)
            {
                memory_type memoryType = Picoboot.PBC_get_memory_type(coalescedRange.Start, device.Model);
                Debug.Assert(Picoboot.PBC_get_memory_type(coalescedRange.End, device.Model) == memoryType);
                AddressRange targetRange = coalescedRange;

                if (memoryType == memory_type.flash)
                {
                    targetRange = targetRange.GetAligned(Picoboot.FLASH_SECTOR_ERASE_SIZE);
                    device.ExitXip();
                    device.FlashErase(targetRange);
                }

                double sizeKibibytes = (double)targetRange.Size / 1024.0;
                Console.WriteLine($"Writing {memoryType.FriendlyName()} region {targetRange} - {sizeKibibytes:N} KiB...");
                using ProgressBar<double> progress = new(sizeKibibytes, "KiB", isEnabled: showProgress);
                while (targetRange.Size > 0)
                {
                    uint chunkSize = Math.Min((uint)_buffer.Length, targetRange.Size);
                    Span<byte> buffer = _buffer.Slice(0, (int)chunkSize);
                    uf2Reader.Read(targetRange.Start, buffer, fillHolesWithZero: true);
                    device.Write(targetRange.Start, buffer);
                    progress.ReportProgress((double)buffer.Length / 1024.0);
                    targetRange = new AddressRange(targetRange.Start + chunkSize, targetRange.End);
                }
            }

            Console.WriteLine($"Upload completed in {Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds:N} seconds");
        }
    }
}
