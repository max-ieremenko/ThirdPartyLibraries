namespace ThirdPartyLibraries.Domain;

public interface IPackageLoaderFactory
{
    IPackageLoader? TryCreate(IPackageReference reference);
}