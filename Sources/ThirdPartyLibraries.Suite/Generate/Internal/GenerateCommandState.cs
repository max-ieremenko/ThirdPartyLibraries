using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class GenerateCommandState
{
    public const string LicensesDirectory = "Licenses";

    private readonly Dictionary<string, FileSource> _sourceByFileName = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<LicenseCode, LicenseNotices> LicenseByCode { get; } = new();

    public List<PackageNotices> Packages { get; } = new();

    public ThirdPartyNoticesLicenseContext CreateLicenseContext(LicenseCode code, ILicenseFileNameResolver fileNameResolver)
    {
        var source = LicenseByCode[code];
        var result = new ThirdPartyNoticesLicenseContext
        {
            FullName = source.FullName
        };

        foreach (var href in source.HRefs)
        {
            result.HRefs.Add(href.ToString());
        }

        foreach (var file in source.Files)
        {
            var fileName = RememberFile(fileNameResolver, file.Hash);
            result.FileNames.Add(fileName);
        }

        result.HRefs.Sort();
        result.FileNames.Sort();
        return result;
    }

    public ThirdPartyNoticesPackageContext CreatePackageContext(
        PackageNotices source,
        ThirdPartyNoticesLicenseContext license,
        ILicenseFileNameResolver fileNameResolver)
    {
        var packageLicense = new ThirdPartyNoticesPackageLicenseContext { FullName = license.FullName };

        if (source.LicenseHRef == null)
        {
            packageLicense.HRefs.AddRange(license.HRefs);
        }
        else
        {
            packageLicense.HRefs.Add(source.LicenseHRef.ToString());
        }

        if (source.LicenseFile == null)
        {
            packageLicense.FileNames.AddRange(license.FileNames);
        }
        else
        {
            var fileName = RememberFile(fileNameResolver, source.LicenseFile.Hash);
            packageLicense.FileNames.Add(fileName);
        }

        return new ThirdPartyNoticesPackageContext
        {
            Name = source.Name,
            Version = source.Version,
            License = license,
            PackageLicense = packageLicense,
            Copyright = source.Copyright,
            HRef = source.HRef.ToString(),
            Author = source.Author,
            ThirdPartyNotices = source.ThirdPartyNotices
        };
    }

    public async Task CopyFileAsync(IStorage storage, string directoryName, string fileName, CancellationToken token)
    {
        var source = _sourceByFileName[fileName];
        
        Stream? sourceStream;
        if (source.LicenseCode != null)
        {
            sourceStream = await storage.OpenLicenseFileReadAsync(source.LicenseCode, source.OriginalFileName, token).ConfigureAwait(false);
        }
        else
        {
            sourceStream = await storage.OpenLibraryFileReadAsync(source.Library!.Value, source.OriginalFileName, token).ConfigureAwait(false);
        }

        var fullPath = Path.Combine(directoryName, fileName);
        using (sourceStream)
        using (var destinationStream = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite))
        {
            await sourceStream!.CopyToAsync(destinationStream, token).ConfigureAwait(false);
        }
    }

    private string RememberFile(ILicenseFileNameResolver fileNameResolver, ArrayHash hash)
    {
        var source = fileNameResolver.ResolveFileSource(hash);
        var fileName = LicensesDirectory + "/" + source.ReportFileName;

        _sourceByFileName.TryAdd(fileName, source);
        return fileName;
    }
}