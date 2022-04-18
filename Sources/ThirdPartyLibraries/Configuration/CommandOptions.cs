using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Configuration;

public static class CommandOptions
{
    public const string CommandUpdate = "update";
    public const string CommandRefresh = "refresh";
    public const string CommandValidate = "validate";
    public const string CommandGenerate = "generate";

    public const string OptionHelp = "help";
    public const string OptionAppName = "appName";
    public const string OptionSource = "source";
    public const string OptionRepository = "repository";
    public const string OptionTo = "to";
    public const string OptionGitHubToken = "github.com:personalAccessToken";
    public const string OptionTitle = "title";

    internal const string UserSecretsId = "c903410c-3d05-49fe-bc8b-b95a2f4dfc69";
    internal const string EnvironmentVariablePrefix = "ThirdPartyLibraries:";

    internal static bool IsSource(in this CommandOption option, out string value)
    {
        if (OptionSource.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = FileTools.RootPath(option.Value);
            return true;
        }

        value = null;
        return false;
    }

    internal static bool IsTo(in this CommandOption option, out string value)
    {
        if (OptionTo.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = FileTools.RootPath(option.Value);
            return true;
        }

        value = null;
        return false;
    }

    internal static bool IsAppName(in this CommandOption option, out string value)
    {
        if (OptionAppName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = option.Value;
            return true;
        }

        value = null;
        return false;
    }

    internal static bool IsRepository(in this CommandOption option, out string value)
    {
        if (OptionRepository.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = option.Value;
            return true;
        }

        value = null;
        return false;
    }

    internal static bool IsGitHubToken(in this CommandOption option, out string value)
    {
        if (OptionGitHubToken.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = option.Value;
            return true;
        }

        value = null;
        return false;
    }

    internal static bool IsTitle(in this CommandOption option, out string value)
    {
        if (OptionTitle.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            value = option.Value;
            return true;
        }

        value = null;
        return false;
    }

    internal static void AssertMissing(string optionName, bool condition)
    {
        if (condition)
        {
            throw new InvalidOperationException("Missing option [{0}].".FormatWith(optionName));
        }
    }

    internal static void AssertDuplicated(string optionName, bool condition)
    {
        if (condition)
        {
            throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(optionName));
        }
    }

    internal static void AssertUnknown(string optionName)
    {
        throw new InvalidOperationException("Option [{0}] is not supported.".FormatWith(optionName));
    }

    private static void AssertNonEmpty(in CommandOption option)
    {
        if (string.IsNullOrEmpty(option.Value))
        {
            throw new InvalidOperationException("Missing value for option [{0}].".FormatWith(option.Name));
        }
    }
}