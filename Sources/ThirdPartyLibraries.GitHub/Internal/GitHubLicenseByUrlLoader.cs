using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Domain;

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
        var content = await _repository.GetAsJsonAsync(url, token).ConfigureAwait(false);
        if (content == null)
        {
            return null;
        }

        var encoding = content.Value<string>("encoding");
        if (!"base64".Equals(encoding, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"GitHub encoding {encoding} is not supported.");
        }

        var license = content.Value<JObject>("license")!;
        var spdxId = license.Value<string>("spdx_id");
        var fileName = content.Value<string>("name");

        return new LicenseSpec(LicenseSpecSource.UserDefined, spdxId!)
        {
            HRef = url,
            FullName = "NOASSERTION".Equals(spdxId, StringComparison.OrdinalIgnoreCase) ? null : license.Value<string>("name")!,
            FileContent = Convert.FromBase64String(content.Value<string>("content")!),
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
        var content = await _repository.GetAsJsonAsync(url, token).ConfigureAwait(false);
        if (content == null)
        {
            return null;
        }

        return new LicenseSpec(LicenseSpecSource.Shared, content.Value<string>("spdx_id")!)
        {
            HRef = url,
            FullName = content.Value<string>("name")!,
            FileContent = Encoding.UTF8.GetBytes(content.Value<string>("body")!),
            FileExtension = ".txt"
        };
    }
}