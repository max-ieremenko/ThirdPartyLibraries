using System;
using System.Collections.Generic;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Configuration;

internal sealed class CommandLine
{
    public string Command { get; set; }

    public IList<CommandOption> Options { get; } = new List<CommandOption>();

    public static CommandLine Parse(params string[] args)
    {
        var result = new CommandLine();
        if (args.IsNullOrEmpty())
        {
            return result;
        }

        string lastOption = null;

        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg))
            {
                continue;
            }

            if (result.Command == null)
            {
                result.Command = arg;
                continue;
            }

            if (TryOption(arg, out var option))
            {
                if (lastOption != null)
                {
                    result.Options.Add(new CommandOption(lastOption));
                }

                lastOption = option;
                continue;
            }

            if (lastOption == null)
            {
                throw new InvalidOperationException("Invalid option [{0}].".FormatWith(arg));
            }

            result.Options.Add(new CommandOption(lastOption, arg));
            lastOption = null;
        }

        if (lastOption != null)
        {
            result.Options.Add(new CommandOption(lastOption));
        }

        return result;
    }

    public static CommandLine Parse(
        string command,
        IList<(string Name, string Value)> options)
    {
        var result = new CommandLine { Command = command };

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            result.Options.Add(new CommandOption(option.Name, option.Value));
        }

        return result;
    }

    private static bool TryOption(string value, out string option)
    {
        if (value.StartsWith("-", StringComparison.Ordinal) && value.Length > 1)
        {
            option = value.Substring(1);
            return true;
        }

        option = null;
        return false;
    }
}