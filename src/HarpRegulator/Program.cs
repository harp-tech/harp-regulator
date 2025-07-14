using Harp.Devices.Pico;
using HarpRegulator;
using PicobootConnection.LibUsb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

if (Debugger.IsAttached)
    Console.Clear();

HelpCommand help = new()
{
    AllCommands =
    [
        new ListDevicesCommand(),
        new UploadFirmwareCommand(),
        new InspectCommand(),
        new InstallDriversCommand(),
    ]
};

Queue<string> arguments = new(args);

if (arguments.Count == 0 || CommandBase.IsHelpArgument(arguments.Peek()))
{
    help.ShowHelp(null);
    return 0;
}

string commandVerb = arguments.Dequeue();
if (help.TryGetCommand(commandVerb) is not CommandBase command)
{
    help.ShowUnknownCommandError(commandVerb);
    return 1;
}

try
{
    switch (command.Execute(arguments))
    {
        case CommandResult.Success:
            return 0;
        case CommandResult.Failure:
            return 1;
        case CommandResult.ShowHelp:
            help.ShowHelp(command);
            return 1;
        default:
            throw new UnreachableException();
    }
}
finally
{
    PicobootDevice.DisposeAll();
    LibusbManager.DisposeIfNeeded();
}
