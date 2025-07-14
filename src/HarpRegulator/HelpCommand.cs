using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace HarpRegulator;

internal sealed class HelpCommand : CommandBase
{
    private ImmutableArray<CommandBase> _AllCommands;
    public required ImmutableArray<CommandBase> AllCommands
    {
        get => _AllCommands;
        init => _AllCommands = value.Add(this);
    }

    public override string Verb => "help";
    public override string Description => "Show command line help.";
    public override string UsageHelp => "help [command-verb]";
    public override string? ArgumentsHelp => null;

    public CommandBase? TryGetCommand(string verb)
        => AllCommands.FirstOrDefault(c => c.Verb.Equals(verb, StringComparison.InvariantCultureIgnoreCase));

    public override CommandResult Execute(Queue<string> arguments)
    {
        switch (arguments.Count)
        {
            case > 1:
            {
                Console.Error.WriteLine("Too many arguments specified.");
                Console.Error.WriteLine();
                ShowHelp(this);
                return CommandResult.Failure;
            }
            case 1:
            {
                string verb = arguments.Dequeue();
                if (TryGetCommand(verb) is CommandBase focus)
                    ShowHelp(focus);
                else
                    ShowUnknownCommandError(verb);
                return CommandResult.Failure;
            }
            case 0:
            {
                ShowHelp(null);
                return CommandResult.Success;
            }
            default:
                throw new UnreachableException();
        }
    }

    public void ShowUnknownCommandError(string verb)
    {
        Console.Error.WriteLine($"Unknown command '{verb}', possible commands are:");
        foreach (CommandBase command in AllCommands.OrderBy(c => c.Verb))
            Console.Error.WriteLine($"  {command.Verb}");
        Console.Error.WriteLine();
    }

    public void ShowHelp(CommandBase? focus)
    {
        void WriteWrapped(string helpText = "", string linePrefix = "")
        {
            if (helpText is "")
            {
                Console.WriteLine();
                return;
            }

            foreach (string _line in helpText.Split('\n'))
            {
                ReadOnlySpan<char> line = _line.AsSpan().TrimEnd();
                ReadOnlySpan<char> message = line.TrimStart(' ');
                ReadOnlySpan<char> indent = line.Slice(0, line.Length - message.Length);
                int lineLength = DisplayWidth - indent.Length - linePrefix.Length;

                do
                {
                    ReadOnlySpan<char> messagePart = message;
                    if (messagePart.Length > lineLength)
                    {
                        messagePart = messagePart.Slice(0, lineLength);
                        int wordBoundary = messagePart.LastIndexOf(' ');
                        if (wordBoundary > 0)
                            messagePart = messagePart.Slice(0, wordBoundary);
                    }

                    Console.WriteLine($"{indent}{linePrefix}{messagePart}");
                    message = message.Slice(messagePart.Length).TrimStart(' ');
                }
                while (message.Length > 0);
            }
        }

        WriteWrapped
        (
            """
             _  _                 ___               _      _
            | || |__ _ _ _ _ __  | _ \___ __ _ _  _| |__ _| |_ ___ _ _
            | __ / _` | '_| '_ \ |   / -_) _` | || | / _` |  _/ _ \ '_|
            |_||_\__,_|_| | .__/ |_|_\___\__, |\_,_|_\__,_|\__\___/_|
                          |_|            |___/
            """
        );


        if (focus is null)
        {
            WriteWrapped();
            WriteWrapped("Usage:");
            foreach (CommandBase command in AllCommands)
            {
                if (command.UsageHelp is null)
                    continue;

                WriteWrapped(command.UsageHelp, "    HarpRegulator ");
            }

            WriteWrapped();
            WriteWrapped("Commands:");
            int maxVerbLength = AllCommands.Select(c => c.Verb.Length).Max();
            foreach (CommandBase command in AllCommands)
            {
                string padding = new String(' ', maxVerbLength - command.Verb.Length);
                ReadOnlySpan<char> description = command.Description.ReplaceLineEndings("\n");
                string? remainingDescription = null;
                int lineBreakIndex = description.IndexOf('\n');
                if (lineBreakIndex >= 0)
                {
                    remainingDescription = description.Slice(lineBreakIndex + 1).ToString();
                    description = description.Slice(0, lineBreakIndex);
                }
                WriteWrapped($"{padding}{command.Verb} - {description}", "    ");
                if (remainingDescription is not null)
                    WriteWrapped(remainingDescription.ToString(), new String(' ', 4 + maxVerbLength + 3));
            }

            foreach (CommandBase command in AllCommands)
            {
                if (command.ArgumentsHelp is null)
                    continue;

                WriteWrapped();
                WriteWrapped($"Arguments and flags for the {command.Verb} command:");
                WriteWrapped(command.ArgumentsHelp, "    ");
            }
        }
        else
        {
            WriteWrapped();
            WriteWrapped(focus.Verb);
            WriteWrapped(focus.Description, "    ");

            if (focus.UsageHelp is not null)
            {
                WriteWrapped();
                WriteWrapped("Command usage:");
                WriteWrapped(focus.UsageHelp, "    HarpRegulator ");
            }

            if (focus.ArgumentsHelp is not null)
            {
                WriteWrapped();
                WriteWrapped("Arguments and flags:");
                WriteWrapped(focus.ArgumentsHelp, "    ");
            }
        }

        WriteWrapped();
        WriteWrapped(CommonArgumentHelp);
    }
}
