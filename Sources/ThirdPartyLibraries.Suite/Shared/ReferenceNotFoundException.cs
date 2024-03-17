using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Shared;

internal sealed class ReferenceNotFoundException : ApplicationException, IApplicationException
{
    public ReferenceNotFoundException(LibraryId[] libraries)
        : base(BuildMessage(libraries))
    {
        Libraries = libraries;
    }

    public LibraryId[] Libraries { get; }

    public void Log(ILogger logger)
    {
        logger.Info("The following packages were not found:");
        using (logger.Indent())
        {
            for (var i = 0; i < Libraries.Length; i++)
            {
                var library = Libraries[i];
                logger.Info($"{library.Name} {library.Version} from {library.SourceCode}");
            }
        }
    }

    private static string BuildMessage(LibraryId[] libraries)
    {
        var result = new StringBuilder()
            .Append("The following packages were not found:");

        for (var i = 0; i < libraries.Length; i++)
        {
            var library = libraries[i];
            result
                .AppendLine()
                .Append("   ")
                .Append(library.Name)
                .Append(" ")
                .Append(library.Version)
                .Append(" from ")
                .Append(library.SourceCode);
        }

        return result.ToString();
    }
}