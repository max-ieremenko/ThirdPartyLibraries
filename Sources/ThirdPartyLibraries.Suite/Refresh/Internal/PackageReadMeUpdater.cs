using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Refresh.Internal;

internal sealed class PackageReadMeUpdater : IPackageReadMeUpdater
{
    private readonly IStorage _storage;
    private readonly IPackageSpecLoader _specLoader;

    public PackageReadMeUpdater(IStorage storage, IPackageSpecLoader specLoader)
    {
        _storage = storage;
        _specLoader = specLoader;
    }

    public Task<RootReadMePackageContext?> UpdateAsync(LibraryId id, CancellationToken token)
    {
        if (id.IsCustomSource())
        {
            return UpdateCustomAsync(id, token);
        }

        return UpdateOtherAsync(id, token);
    }

    private static string JoinUsedBy(IEnumerable<Application> applications)
    {
        // apps are already sorted by UpdateCommand
        var list = applications
            .Select(i => i.InternalOnly ? i.Name + " internal" : i.Name);

        return string.Join(", ", list);
    }

    private static string JoinTargetFrameworks(IEnumerable<Application> applications)
    {
        var list = applications
            .SelectMany(i => i.TargetFrameworks ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => i, StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", list);
    }

    private static void AddLicenses(List<LibraryLicense> source, IList<LibraryLicense> dest)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var license = source[i].Clone();
            if (string.IsNullOrEmpty(license.Code))
            {
                license.Code = "Unknown";
            }

            // skip file reference
            if (license.HRef != null && !Uri.TryCreate(license.HRef, UriKind.Absolute, out _))
            {
                license.HRef = null;
            }

            dest.Add(license);
        }
    }

    private async Task<RootReadMePackageContext?> UpdateCustomAsync(LibraryId id, CancellationToken token)
    {
        var index = await _storage.ReadCustomLibraryIndexJsonAsync(id, token).ConfigureAwait(false);
        if (index == null)
        {
            return null;
        }

        var result = new RootReadMePackageContext
        {
            Source = LibraryIdExtensions.CustomPackageSources,
            SourceHRef = index.HRef,
            Name = index.Name,
            Version = index.Version,
            UsedBy = JoinUsedBy(index.UsedBy),
            LocalHRef = _storage.GetPackageLocalHRef(id)
        };

        AddLicenseStatus(result, new LicenseConclusion(index.LicenseCode, PackageLicenseApprovalStatus.CodeApproved));

        return result;
    }

    private async Task<RootReadMePackageContext?> UpdateOtherAsync(LibraryId id, CancellationToken token)
    {
        var index = await _storage.ReadLibraryIndexJsonAsync(id, token).ConfigureAwait(false);
        var spec = await _specLoader.LoadAsync(id, token).ConfigureAwait(false);
        
        if (index == null || spec == null)
        {
            return null;
        }

        var source = _specLoader.ResolveParser(id).NormalizePackageSource(spec, index.Source);

        var result = new RootReadMePackageContext
        {
            Source = source.Text,
            SourceHRef = source.DownloadUrl.ToString(),
            Name = spec.GetName(),
            Version = spec.GetVersion(),
            UsedBy = JoinUsedBy(index.UsedBy),
            LocalHRef = _storage.GetPackageLocalHRef(id)
        };

        AddLicenseStatus(result, index.License);

        var context = new LibraryReadMeContext
        {
            Name = result.Name,
            Version = result.Version,
            Description = spec.GetDescription(),
            UsedBy = result.UsedBy,
            TargetFrameworks = JoinTargetFrameworks(index.UsedBy),
            HRef = result.SourceHRef
        };

        AddLicenseStatus(id, context, result.License, result.IsApproved);
        AddDependencies(id, index.UsedBy, context.Dependencies);
        AddLicenses(index.Licenses, context.Licenses);

        context.Remarks = await _storage.ReadRemarksFileAsync(id, token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(context.Remarks))
        {
            context.Remarks = "no remarks";
        }

        context.ThirdPartyNotices = await _storage.ReadThirdPartyNoticesFileAsync(id, token).ConfigureAwait(false);

        await _storage.WriteLibraryReadMeAsync(id, context, token).ConfigureAwait(false);

        return result;
    }

    private void AddDependencies(LibraryId id, IEnumerable<Application> applications, IList<LibraryReadMeDependencyContext> dependencies)
    {
        var list = applications
            .SelectMany(i => i.Dependencies ?? [])
            .Select(i => new LibraryId(id.SourceCode, i.Name, i.Version))
            .Distinct()
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Version, StringComparer.OrdinalIgnoreCase);

        foreach (var dependency in list)
        {
            dependencies.Add(new LibraryReadMeDependencyContext
            {
                Name = dependency.Name,
                Version = dependency.Version,
                LocalHRef = _storage.GetPackageLocalHRef(dependency, id)
            });
        }
    }

    private void AddLicenseStatus(RootReadMePackageContext context, LicenseConclusion license)
    {
        var code = LicenseCode.FromText(license.Code);
        if (code.IsEmpty)
        {
            return;
        }

        context.License = license.Code;
        context.LicenseLocalHRef = _storage.GetLicenseLocalHRef(code.Codes[0]);
        context.LicenseMarkdownExpression = code.ReplaceCodes(i => AddHrefToLicenseCode(i, null));
        context.IsApproved = PackageLicenseApprovalStatus.IsApproved(license.Status) || PackageLicenseApprovalStatus.IsAutomaticallyApproved(license.Status);
    }

    private void AddLicenseStatus(LibraryId id, LibraryReadMeContext context, string? licenseCode, bool isApproved)
    {
        var code = LicenseCode.FromText(licenseCode);
        if (code.IsEmpty)
        {
            context.LicenseCode = "Unknown";
        }
        else
        {
            context.LicenseCode = licenseCode;
            context.LicenseLocalHRef = _storage.GetLicenseLocalHRef(code.Codes[0], id);
            context.LicenseMarkdownExpression = code.ReplaceCodes(i => AddHrefToLicenseCode(i, id));
            context.LicenseDescription = isApproved ? null : "has to be approved";
        }
    }

    private string AddHrefToLicenseCode(string code, LibraryId? relativeTo)
    {
        var href = _storage.GetLicenseLocalHRef(code, relativeTo);
        return $"[{code}]({href})";
    }
}