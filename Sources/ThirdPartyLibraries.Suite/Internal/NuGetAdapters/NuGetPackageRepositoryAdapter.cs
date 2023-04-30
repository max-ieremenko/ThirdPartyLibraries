using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

internal sealed class NuGetPackageRepositoryAdapter : PackageRepositoryAdapterBase
{
    public NuGetPackageRepositoryAdapter(IServiceProvider serviceProvider)
    {
        Api = serviceProvider.GetRequiredService<INuGetApi>();
        ServiceProvider = serviceProvider;
    }

    public INuGetApi Api { get; }

    public IServiceProvider ServiceProvider { get; }

    protected override async Task AppendSpecAttributesAsync(LibraryId id, Package package, CancellationToken token)
    {
        var spec = await ReadSpecAsync(id, token).ConfigureAwait(false);
        package.Name = spec.Id;
        package.Version = spec.Version;
        package.Description = spec.Description;
        (package.HRefText, package.HRef) = GetUrlResolver(package.HRef).GetUserUrl(spec.Id, spec.Version, package.HRef, spec.Repository?.Url);
        package.Author = spec.Authors;
        package.Copyright = spec.Copyright;
    }

    private async Task<NuGetSpec> ReadSpecAsync(LibraryId id, CancellationToken token)
    {
        using (var specContent = await Storage.OpenLibraryFileReadAsync(id, NuGetConstants.RepositorySpecFileName, token).ConfigureAwait(false))
        {
            return Api.ParseSpec(specContent);
        }
    }

    private INuGetPackageUrlResolver GetUrlResolver(string source)
    {
        INuGetPackageUrlResolver result = null;
        if (Uri.TryCreate(source, UriKind.Absolute, out var url))
        {
            result = ServiceProvider.GetKeyedService<INuGetPackageUrlResolver>(url.Host);
        }

        if (result == null)
        {
            result = ServiceProvider.GetRequiredKeyedService<INuGetPackageUrlResolver>(KnownHosts.NuGetApi);
        }

        return result;
    }
}