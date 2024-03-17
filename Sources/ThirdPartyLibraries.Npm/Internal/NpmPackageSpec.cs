using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageSpec : IPackageSpec
{
    private const string Unlicensed = "UNLICENSED";
    private const string FileLicense = "SEE LICENSE IN ";
    
    private readonly JObject _content;

    public NpmPackageSpec(JObject content)
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
        var content = stream.JsonDeserialize<JObject>();
        return new NpmPackageSpec(content);
    }

    public string GetName()
    {
        return _content.Value<string>("name")!;
    }

    public string GetVersion()
    {
        return _content.Value<string>("version")!;
    }

    public string? GetDescription()
    {
        return _content.Value<string>("description");
    }

    public (PackageSpecLicenseType Type, string? Value) GetLicense()
    {
        var license = _content.GetValue("license");
        if (license != null)
        {
            if (license is JObject obj)
            {
                license = obj.GetValue("type");
            }

            if (license is JValue value)
            {
                return ParseLicenseAsExpression(value.Value as string);
            }

            return (PackageSpecLicenseType.NotDefined, null);
        }

        string? code = null;
        if (_content.GetValue("licenses") is JArray licenses)
        {
            var codes = licenses
                .OfType<JObject>()
                .Select(i => (i.GetValue("type") as JValue)?.Value as string)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 1)
            {
                code = codes[0];
            }
            else if (codes.Count > 1)
            {
                code = "(" + string.Join(" OR ", codes) + ")";
            }
        }

        return (code == null ? PackageSpecLicenseType.NotDefined : PackageSpecLicenseType.Expression, code);
    }

    public string? GetRepositoryUrl()
    {
        string? repositoryUrl = null;

        var repository = _content.GetValue("repository");
        if (repository is JObject obj)
        {
            repositoryUrl = obj.Value<string>("url");
        }
        else if (repository is JValue url)
        {
            repositoryUrl = url.Value as string;
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

    public string? GetHomePage()
    {
        return _content.Value<string>("homepage");
    }

    public string? GetCopyright() => null;

    public string? GetAuthor()
    {
        var author = _content.GetValue("author") ?? _content.GetValue("contributors");
        if (author == null)
        {
            return null;
        }

        if (author is JArray array)
        {
            var result = new StringBuilder();
            foreach (var item in array)
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

        return ParseAuthorName(author);
    }

    public IEnumerable<LibraryId> GetDependencies() => ParseDependencies((JObject?)_content.GetValue("dependencies"));

    public IEnumerable<LibraryId> GetDevDependencies() => ParseDependencies((JObject?)_content.GetValue("devDependencies"));

    private static IEnumerable<LibraryId> ParseDependencies(JObject? root)
    {
        if (root == null)
        {
            return Array.Empty<LibraryId>();
        }

        var result = new List<LibraryId>();
        foreach (var property in root.Properties())
        {
            var version = (string?)property.Value;
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

    private static string? ParseAuthorName(JToken value)
    {
        if (value is JObject obj)
        {
            return obj.Value<string>("name");
        }

        if (value is not JValue name)
        {
            return null;
        }

        var singleString = ((string?)name.Value).AsSpan().Trim();
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