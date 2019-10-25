using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class RepositoryLicense
    {
        public RepositoryLicense(string code, bool requiresApproval)
        {
            code.AssertNotNull(nameof(code));

            Code = code;
            RequiresApproval = requiresApproval;
        }

        public string Code { get; }

        public bool RequiresApproval { get; }
    }
}