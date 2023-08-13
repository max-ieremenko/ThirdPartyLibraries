using Moq;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries;

public static class PackageReferenceMock
{
    public static Mock<IPackageReference> Create(LibraryId id)
    {
        var result = new Mock<IPackageReference>(MockBehavior.Strict);
        result
            .SetupGet(r => r.Id)
            .Returns(id);
        return result;
    }
}