using System;
using System.ComponentModel;
using System.Text;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Validate.Internal;

internal sealed class RepositoryValidationException : ApplicationException, IApplicationException
{
    public RepositoryValidationException(RepositoryValidationError[] errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    public RepositoryValidationError[] Errors { get; }

    public void Log(ILogger logger)
    {
        for (var i = 0; i < Errors.Length; i++)
        {
            var error = Errors[i];
            var issue = GetIssue(error.Issue, error.AppName);

            logger.Info($"The following libraries {issue}:");
            using (logger.Indent())
            {
                for (var j = 0; j < error.Libraries.Length; j++)
                {
                    var lib = error.Libraries[j];
                    logger.Info($"{lib.Name} {lib.Version} from {lib.SourceCode}");
                }
            }
        }
    }

    private static string BuildMessage(RepositoryValidationError[] errors)
    {
        var result = new StringBuilder();

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];
            if (result.Length > 0)
            {
                result.AppendLine();
            }

            result
                .Append("The following libraries ")
                .Append(GetIssue(error.Issue, error.AppName))
                .Append(":");

            for (var j = 0; j < error.Libraries.Length; j++)
            {
                var lib = error.Libraries[j];
                result
                    .AppendLine()
                    .Append("   ")
                    .Append(lib.Name)
                    .Append(" ")
                    .Append(lib.Version)
                    .Append(" from ")
                    .Append(lib.SourceCode);
            }
        }

        return result.ToString();
    }

    private static string GetIssue(ValidationResult error, string appName)
    {
        switch (error)
        {
            case ValidationResult.IndexNotFound:
                return "not found in the repository";

            case ValidationResult.NotAssignedToIndex:
                return $"are not assigned to {appName}";

            case ValidationResult.NoLicenseCode:
                return "have no license";

            case ValidationResult.LicenseNotFound:
                return "have a license that they did not find";

            case ValidationResult.LicenseNotApproved:
                return "are not approved";

            case ValidationResult.NoThirdPartyNotices:
                return "have no third party notices";

            case ValidationResult.ReferenceNotFound:
                return $"are assigned to {appName}, but references not found in the sources";
        }

        throw new InvalidEnumArgumentException(nameof(error), (int)error, typeof(ValidationResult));
    }
}