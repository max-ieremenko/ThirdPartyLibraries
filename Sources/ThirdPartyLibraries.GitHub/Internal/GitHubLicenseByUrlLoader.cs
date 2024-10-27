using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.GitHub.Internal.Domain;

namespace ThirdPartyLibraries.GitHub.Internal;

internal sealed class GitHubLicenseByUrlLoader : ILicenseByUrlLoader
{
    private readonly IGitHubRepository _repository;
    private readonly Dictionary<RepositoryId, LicenseSpec?> _specByRepository;
    private readonly Dictionary<string, LicenseSpec?> _specByCode;

    public GitHubLicenseByUrlLoader(IGitHubRepository repository)
    {
        _repository = repository;
        _specByRepository = new Dictionary<RepositoryId, LicenseSpec?>();
        _specByCode = new Dictionary<string, LicenseSpec?>(StringComparer.OrdinalIgnoreCase);
    }

    public Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token)
    {
        if (GitHubUrlParser.TryParseRepository(url, out var owner, out var repository))
        {
            return GetOrLoadRepositoryLicenseAsync(new RepositoryId(owner, repository), token);
        }

        if (GitHubUrlParser.TryParseLicenseCode(url, out var code))
        {
            return GetOrLoadLicenseCodeAsync(code, token);
        }

        return Task.FromResult((LicenseSpec?)null);
    }

    private async Task<LicenseSpec?> GetOrLoadRepositoryLicenseAsync(RepositoryId repositoryId, CancellationToken token)
    {
        if (_specByRepository.TryGetValue(repositoryId, out var result))
        {
            return result;
        }

        var requestUri = "https://" + GitHubUrlParser.HostApi + $"/repos/{repositoryId.Owner}/{repositoryId.Name}/license";

        result = await LoadRepositoryLicenseAsync(requestUri, token).ConfigureAwait(false);
        _specByRepository.Add(repositoryId, result);

        return result;
    }

    private async Task<LicenseSpec?> LoadRepositoryLicenseAsync(string url, CancellationToken token)
    {
        var license = await _repository.GetAsJsonAsync(url, DomainJsonSerializerContext.Default.GitHubRepositoryLicense, token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(license?.License?.SpdxId))
        {
            return null;
        }

        if (!"base64".Equals(license.Encoding, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"GitHub encoding {license.Encoding} is not supported.");
        }

        var spdxId = license.License.SpdxId;
        var fileName = license.Name;

        return new LicenseSpec(LicenseSpecSource.UserDefined, spdxId)
        {
            HRef = url,
            FullName = "NOASSERTION".Equals(spdxId, StringComparison.OrdinalIgnoreCase) ? null : license.License.Name,
            FileContent = Convert.FromBase64String(license.Content!),
            FileName = fileName,
            FileExtension = Path.GetExtension(fileName)
        };
    }

    private async Task<LicenseSpec?> GetOrLoadLicenseCodeAsync(string code, CancellationToken token)
    {
        if (_specByCode.TryGetValue(code, out var result))
        {
            return result;
        }

        var requestUri = "https://" + GitHubUrlParser.HostApi + $"/licenses/{code}";

        result = await LoadLicenseCodeAsync(requestUri, token).ConfigureAwait(false);
        _specByCode.Add(code, result);

        return result;
    }

    private async Task<LicenseSpec?> LoadLicenseCodeAsync(string url, CancellationToken token)
    {
        var content = await _repository.GetAsJsonAsync(url, DomainJsonSerializerContext.Default.GitHubLicense, token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(content?.SpdxId))
        {
            return null;
        }

        return new LicenseSpec(LicenseSpecSource.Shared, content.SpdxId)
        {
            HRef = url,
            FullName = content.Name,
            FileContent = Encoding.UTF8.GetBytes(content.Body!),
            FileExtension = ".txt"
        };
    }
}