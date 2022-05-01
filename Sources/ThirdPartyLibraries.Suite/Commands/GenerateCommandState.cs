using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
    internal sealed class GenerateCommandState
    {
        private const string LicensesDirectory = "Licenses";

        private readonly IDictionary<string, LicenseIndexJson> _indexByCode;
        private readonly IDictionary<string, ThirdPartyNoticesLicenseContext> _licenseByCode;
        private readonly IDictionary<string, LicenseFile> _licenseFileByReportName;

        public GenerateCommandState(IPackageRepository repository, string to, ILogger logger)
        {
            Repository = repository;
            To = to;
            Logger = logger;

            _indexByCode = new Dictionary<string, LicenseIndexJson>(StringComparer.OrdinalIgnoreCase);
            _licenseByCode = new Dictionary<string, ThirdPartyNoticesLicenseContext>(StringComparer.OrdinalIgnoreCase);
            _licenseFileByReportName = new Dictionary<string, LicenseFile>(StringComparer.OrdinalIgnoreCase);
        }

        public IPackageRepository Repository { get; }

        public string To { get; }

        public ILogger Logger { get; }

        public IEnumerable<ThirdPartyNoticesLicenseContext> Licenses => _licenseByCode.Values;

        public IEnumerable<string> LicenseFiles => _licenseFileByReportName.Keys;

        public async Task<ThirdPartyNoticesLicenseContext> GetLicensesAsync(string licenseExpression, CancellationToken token)
        {
            if (_licenseByCode.TryGetValue(licenseExpression, out var result))
            {
                return result;
            }

            var codes = LicenseExpression.GetCodes(licenseExpression);
            result = new ThirdPartyNoticesLicenseContext { FullName = licenseExpression };

            foreach (var code in codes)
            {
                var index = await LoadLicenseIndexAsync(code, token).ConfigureAwait(false);
                if (index != null)
                {
                    result.HRefs.Add(index.HRef);
                    if (!index.FileName.IsNullOrEmpty())
                    {
                        result.FileNames.Add(index.FileName);
                    }

                    if (codes.Count == 1)
                    {
                        result.FullName = index.FullName;
                    }
                }
            }

            _licenseByCode.Add(licenseExpression, result);
            return result;
        }

        public void CleanUpLicensesDirectory()
        {
            var path = Path.Combine(To, LicensesDirectory);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public Task CopyLicenseFileAsync(string fileName, CancellationToken token)
        {
            var file = _licenseFileByReportName[fileName];
            return CopyLicenseFileRecursivelyAsync(file, new HashSet<string>(StringComparer.OrdinalIgnoreCase), token);
        }

        private async Task CopyLicenseFileRecursivelyAsync(LicenseFile file, HashSet<string> distinct, CancellationToken token)
        {
            if (!distinct.Add(file.LicenseCode))
            {
                return;
            }

            await CopyLicenseFileContentAsync(file, token).ConfigureAwait(false);

            foreach (var dependency in file.Dependencies)
            {
                var dependencyFile = _licenseFileByReportName[dependency];
                await CopyLicenseFileRecursivelyAsync(dependencyFile, distinct, token).ConfigureAwait(false);
            }
        }

        private async Task CopyLicenseFileContentAsync(LicenseFile file, CancellationToken token)
        {
            if (file.RepositoryName.IsNullOrEmpty())
            {
                return;
            }

            var fileName = Path.Combine(To, file.ReportName);
            if (File.Exists(fileName))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            using (var content = await Repository.Storage.OpenLicenseFileReadAsync(file.LicenseCode, file.RepositoryName, token).ConfigureAwait(false))
            using (var dest = new FileStream(Path.Combine(To, fileName), FileMode.Create, FileAccess.ReadWrite))
            {
                await content.CopyToAsync(dest, token).ConfigureAwait(false);
            }
        }

        private async Task<LicenseIndexJson> LoadLicenseIndexAsync(string code, CancellationToken token)
        {
            if (_indexByCode.TryGetValue(code, out var index))
            {
                return index;
            }

            index = await Repository.Storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
            if (index == null)
            {
                Logger.Info("License {0} not found in the repository.".FormatWith(code));
                _indexByCode.Add(code, null);
                return null;
            }

            var result = new LicenseIndexJson
            {
                FullName = index.FullName.IsNullOrEmpty() ? index.Code : index.FullName,
                HRef = index.HRef
            };

            _indexByCode.Add(code, result);

            var licenseFile = new LicenseFile(code, index.FileName);
            _licenseFileByReportName.Add(licenseFile.ReportName, licenseFile);

            if (!index.FileName.IsNullOrEmpty())
            {
                result.FileName = licenseFile.ReportName;
            }

            if (!index.Dependencies.IsNullOrEmpty())
            {
                foreach (var dependency in index.Dependencies)
                {
                    var dependencyIndex = await LoadLicenseIndexAsync(dependency, token).ConfigureAwait(false);
                    if (!dependencyIndex.FileName.IsNullOrEmpty())
                    {
                        licenseFile.Dependencies.Add(dependencyIndex.FileName);
                    }
                }
            }

            return result;
        }

        private sealed class LicenseFile
        {
            public LicenseFile(string licenseCode, string repositoryName)
            {
                LicenseCode = licenseCode;
                ReportName = "{0}/{1}-{2}".FormatWith(LicensesDirectory, licenseCode, repositoryName.IsNullOrEmpty() ? "dummy.txt" : repositoryName);
                RepositoryName = repositoryName;
                Dependencies = new List<string>();
            }

            public string LicenseCode { get; }

            public string ReportName { get; }

            public string RepositoryName { get; }

            public IList<string> Dependencies { get; }
        }
    }
}
