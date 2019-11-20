using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class RepositoryLicense
    {
        public RepositoryLicense(string code, bool requiresApproval, bool requiresThirdPartyNotices, string[] dependencies)
        {
            code.AssertNotNull(nameof(code));
            dependencies.AssertNotNull(nameof(dependencies));

            Code = code;
            RequiresApproval = requiresApproval;
            RequiresThirdPartyNotices = requiresThirdPartyNotices;
            Dependencies = dependencies;
        }

        public string Code { get; }

        public bool RequiresApproval { get; }

        public bool RequiresThirdPartyNotices { get; }

        public string[] Dependencies { get; }
    }
}