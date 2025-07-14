using Harp.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;

namespace HarpRegulator;

internal sealed class InstallDriversCommand : CommandBase
{
    public override string Verb => "install-drivers";
    public override string Description =>
        """
        Automatically installs drivers required to interact with some Harp devices.
        (Intended to resolve devices stuck in the "Driver Error" state.)
        """;
    public override string? UsageHelp => Verb;
    public override string? ArgumentsHelp => null;

    public override CommandResult Execute(Queue<string> arguments)
    {
        while (arguments.Count > 0)
        {
            string argument = arguments.Dequeue();
            switch (TryHandleCommonArgument(argument))
            {
                case CommonArgumentResult.Handled:
                    continue;
                case CommonArgumentResult.ShowHelp:
                    return CommandResult.ShowHelp;
                case CommonArgumentResult.NotHandled:
                    Console.Error.WriteLine($"Unknown argument '{argument}'");
                    return CommandResult.Failure;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            using (WindowsIdentity id = WindowsIdentity.GetCurrent())
            {
                if (!new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))
                    Console.Error.WriteLine($"⚠  Not running as admin. Installing drivers generally requires running with elevated privileges!{Environment.NewLine}");
            }

            CommandResult result;
            if (DriverHelper.InstallWinUSB(out int successCount, out int failCount, out bool restartRecommended))
            {
                Debug.Assert(failCount == 0);
                if (successCount == 0)
                    Console.WriteLine("No devices were in need of drivers.");
                else
                    Console.WriteLine($"Successfully configured drivers for {successCount} {(successCount == 1 ? "device" : "devices")}");
                result = CommandResult.Success;
            }
            else
            {
                // If we didn't enumerate any devices, another error was already printed.
                int totalDevices = failCount + successCount;
                if (totalDevices > 0)
                    Console.Error.WriteLine($"Failed to configure {failCount}/{totalDevices} {(totalDevices == 1 ? "device" : "devices")}");
                result = CommandResult.Failure;
            }

            if (restartRecommended)
            {
                Console.WriteLine();
                Console.WriteLine("Windows has indicated that a restart is recommended to complete driver installation.");
            }

            return result;
        }
        else
        {
            Console.Error.WriteLine("Command not applicable on this platform.");
            return CommandResult.Failure;
        }
    }
}
