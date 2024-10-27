using System.Text.Json;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Npm.Internal.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageSpec : IPackageSpec
{
    private const string Unlicensed = "UNLICENSED";
    private const string FileLicense = "SEE LICENSE IN ";
    
    private readonly NpmPackageJson _content;

    public NpmPackageSpec(NpmPackageJson content)
    {
        _content = content;
    }

    public static NpmPackageSpec FromFile(string fileName)
    {
        using (var stream = File.OpenRead(fileName))
        {
            return FromStream(stream);
        }
    }

    public static NpmPackageSpec FromStream(Stream stream)
    {
        var content = stream.JsonDeserialize(DomainJsonSerializerContext.Default.NpmPackageJson);
        return new NpmPackageSpec(content);
    }

    public string GetName() => _content.Name!;

    public string GetVersion() => _content.Version!;

    public string? GetDescription() => _content.Description;

    public (PackageSpecLicenseType Type, string? Value) GetLicense()
    {
        static string? GetCode(JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.String)
            {
                return json.GetString();
            }

            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
            {
                return type.GetString();
            }

            return null;
        }

        if (_content.Licenses != null)
        {
            var codes = _content
                .Licenses
                .Select(GetCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string? code = null;
            if (codes.Count == 1)
            {
                code = codes[0];
            }
            else if (codes.Count > 1)
            {
                code = "(" + string.Join(" OR ", codes) + ")";
            }

            return (code == null ? PackageSpecLicenseType.NotDefined : PackageSpecLicenseType.Expression, code);
        }

        return ParseLicenseAsExpression(GetCode(_content.License));
    }

    public string? GetRepositoryUrl()
    {
        string? repositoryUrl = null;

        if (_content.Repository.ValueKind == JsonValueKind.Object && _content.Repository.TryGetProperty("url", out var url))
        {
            repositoryUrl = url.GetString();
        }

        if (_content.Repository.ValueKind == JsonValueKind.String)
        {
            repositoryUrl = _content.Repository.GetString();
        }

        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return null;
        }

        UriBuilder builder;
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out _))
        {
            builder = new UriBuilder(Uri.UriSchemeHttps, "github.com", 443, repositoryUrl);
        }
        else
        {
            builder = new UriBuilder(repositoryUrl);
        }

        if (builder.Scheme.Contains('+'))
        {
            builder.Scheme = builder.Scheme.Substring(builder.Scheme.IndexOf('+') + 1);
        }

        if ("git".Equals(builder.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            builder.Scheme = Uri.UriSchemeHttps;
        }
        else if ("github".Equals(builder.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            builder.Scheme = Uri.UriSchemeHttps;
            builder.Path = "github.com/" + builder.Path;
        }
        else if ("gitlab".Equals(builder.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            builder.Scheme = Uri.UriSchemeHttps;
            builder.Path = "gitlab.com/" + builder.Path;
        }
        else if ("bitbucket".Equals(builder.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            builder.Scheme = Uri.UriSchemeHttps;
            builder.Path = "bitbucket.org/" + builder.Path;
        }
        else if (!Uri.UriSchemeHttps.Equals(builder.Scheme, StringComparison.OrdinalIgnoreCase) && !Uri.UriSchemeHttp.Equals(builder.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return builder.Uri.ToString();
    }

    public string? GetHomePage() => _content.HomePage;

    public string? GetCopyright() => null;

    public string? GetAuthor()
    {
        var author = _content.Author ?? _content.Contributors;
        if (author == null)
        {
            return null;
        }

        if (author.Value.ValueKind == JsonValueKind.Array)
        {
            var result = new StringBuilder();
            foreach (var item in author.Value.EnumerateArray())
            {
                var name = ParseAuthorName(item);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result.Append(", ");
                }

                result.Append(name);
            }

            return result.Length == 0 ? null : result.ToString();
        }

        return ParseAuthorName(author.Value);
    }

    public IEnumerable<LibraryId> GetDependencies() => ParseDependencies(_content.Dependencies);

    public IEnumerable<LibraryId> GetDevDependencies() => ParseDependencies(_content.DevDependencies);

    private static IEnumerable<LibraryId> ParseDependencies(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<LibraryId>();
        }

        var result = new List<LibraryId>();
        foreach (var property in root.EnumerateObject())
        {
            var version = property.Value.GetString();
            if (string.IsNullOrWhiteSpace(version))
            {
                version = "*";
            }

            result.Add(NpmLibraryId.New(property.Name, version));
        }

        return result;
    }

    private static (PackageSpecLicenseType Type, string? Value) ParseLicenseAsExpression(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || Unlicensed.Equals(value, StringComparison.OrdinalIgnoreCase))
        {
            return (PackageSpecLicenseType.NotDefined, null);
        }

        var type = PackageSpecLicenseType.Expression;
        if (value.StartsWith(FileLicense, StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(FileLicense.Length).Trim();
            type = PackageSpecLicenseType.File;
        }

        return (type, value);
    }

    private static string? ParseAuthorName(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return value.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var singleString = value.GetString().AsSpan().Trim();
        if (singleString.IsEmpty)
        {
            return null;
        }

        var index = singleString.IndexOfAny('<', '(');
        if (index > 0)
        {
            singleString = singleString.Slice(0, index).Trim();
        }

        return singleString.ToString();
    }
}