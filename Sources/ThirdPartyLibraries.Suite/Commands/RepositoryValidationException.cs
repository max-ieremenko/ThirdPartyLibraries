using System;
using System.Text;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Commands;

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

            logger.Info("The following libraries {0}:".FormatWith(error.Issue));
            using (logger.Indent())
            {
                for (var j = 0; j < error.Libraries.Length; j++)
                {
                    var lib = error.Libraries[j];
                    logger.Info("{0} {1} from {2}".FormatWith(lib.Name, lib.Version, lib.SourceCode));
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
                .Append(error.Issue)
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
}