using System;
using System.Diagnostics.CodeAnalysis;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Configuration;

public static class CommandOptions
{
    public const string CommandUpdate = "update";
    public const string CommandRefresh = "refresh";
    public const string CommandValidate = "validate";
    public const string CommandGenerate = "generate";
    public const string CommandRemove = "remove";

    public const string OptionHelp = "help";
    public const string OptionAppName = "appName";
    public const string OptionSource = "source";
    public const string OptionRepository = "repository";
    public const string OptionTo = "to";
    public const string OptionGitHubToken = "github.com:personalAccessToken";
    public const string OptionTitle = "title";
    public const string OptionToFileName = "toFileName";
    public const string OptionTemplate = "template";

    internal const string UserSecretsId = "c903410c-3d05-49fe-bc8b-b95a2f4dfc69";
    internal const string EnvironmentVariablePrefix = "ThirdPartyLibraries:";

    internal static bool IsSource(in this CommandOption option, [NotNullWhen(true)] out string? value) => IsPath(option, OptionSource, out value);

    internal static bool IsTo(in this CommandOption option, [NotNullWhen(true)] out string? value) => IsPath(option, OptionTo, out value);

    internal static bool IsAppName(in this CommandOption option, [NotNullWhen(true)] out string? value) => Is(option, OptionAppName, out value);

    internal static bool IsRepository(in this CommandOption option, [NotNullWhen(true)] out string? value) => Is(option, OptionRepository, out value);

    internal static bool IsGitHubToken(in this CommandOption option, [NotNullWhen(true)] out string? value) => Is(option, OptionGitHubToken, out value);

    internal static bool IsTitle(in this CommandOption option, [NotNullWhen(true)] out string? value) => Is(option, OptionTitle, out value);

    internal static bool IsToFileName(in this CommandOption option, [NotNullWhen(true)] out string? value) => Is(option, OptionToFileName, out value);

    internal static bool IsTemplate(in this CommandOption option, [NotNullWhen(true)] out string? value) => IsPath(option, OptionTemplate, out value);

    internal static void AssertMissing(string optionName, bool condition)
    {
        if (condition)
        {
            throw new InvalidOperationException($"Missing option [{optionName}].");
        }
    }

    internal static void AssertDuplicated(string optionName, bool condition)
    {
        if (condition)
        {
            throw new InvalidOperationException($"Option [{optionName}] is duplicated.");
        }
    }

    internal static void AssertUnknown(string optionName)
    {
        throw new InvalidOperationException($"Option [{optionName}] is not supported.");
    }

    private static void AssertNonEmpty(in CommandOption option)
    {
        if (string.IsNullOrEmpty(option.Value))
        {
            throw new InvalidOperationException($"Missing value for option [{option.Name}].");
        }
    }

    private static bool Is(in this CommandOption option, string expectedName, [NotNullWhen(true)] out string? value)
    {
        if (expectedName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = option.Value!;
            return true;
        }

        value = null;
        return false;
    }

    private static bool IsPath(in this CommandOption option, string expectedName, [NotNullWhen(true)] out string? value)
    {
        if (expectedName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
        {
            AssertNonEmpty(option);
            value = FileTools.RootPath(option.Value!);
            return true;
        }

        value = null;
        return false;
    }
}