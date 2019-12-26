using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm
{
    public readonly struct PackageJsonParser
    {
        public const string FileName = "package.json";
        public const string NodeModules = "node_modules";

        private const string Unlicensed = "UNLICENSED";
        private const string FileLicense = "SEE LICENSE IN ";

        public PackageJsonParser(JObject content)
        {
            content.AssertNotNull(nameof(content));

            Content = content;
        }

        public JObject Content { get; }

        public static PackageJsonParser FromFile(string fileName)
        {
            fileName.AssertNotNull(nameof(fileName));

            using (var stream = File.OpenRead(fileName))
            {
                return FromStream(stream);
            }
        }

        public static PackageJsonParser FromStream(Stream stream)
        {
            stream.AssertNotNull(nameof(stream));

            JObject content;

            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                content = (JObject)new JsonSerializer().Deserialize(jsonReader);
            }

            return new PackageJsonParser(content);
        }

        public string GetName() => Content.Value<string>("name");

        public string GetVersion() => Content.Value<string>("version");

        public string GetHomePage() => Content.Value<string>("homepage");

        public string GetAuthors()
        {
            var author = Content.GetValue("author") ?? Content.GetValue("contributors");
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
                    if (name.IsNullOrEmpty())
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

        public string GetDescription() => Content.Value<string>("description");

        public PackageJsonLicense GetLicense()
        {
            var result = new PackageJsonLicense
            {
                Type = "expression"
            };

            var value = (Content.GetValue("license") as JValue)?.Value as string;
            if (value.IsNullOrEmpty() || Unlicensed.EqualsIgnoreCase(value))
            {
                return result;
            }

            if (value.StartsWithIgnoreCase(FileLicense))
            {
                value = value.Substring(FileLicense.Length).Trim();
                result.Type = "file";
            }

            result.Value = value;
            return result;
        }

        public PackageJsonRepository GetRepository()
        {
            var repository = ParseRepository(Content.GetValue("repository"));
            if ((repository?.Url).IsNullOrEmpty())
            {
                return null;
            }

            UriBuilder builder;
            if (!Uri.TryCreate(repository.Url, UriKind.Absolute, out _))
            {
                repository.Type = "git";
                builder = new UriBuilder(Uri.UriSchemeHttps, "github.com", 443, repository.Url);
            }
            else
            {
                builder = new UriBuilder(repository.Url);
            }

            if (builder.Scheme.Contains('+'))
            {
                builder.Scheme = builder.Scheme.Substring(builder.Scheme.IndexOf('+') + 1);
            }

            if ("git".EqualsIgnoreCase(builder.Scheme))
            {
                repository.Type = "git";
                builder.Scheme = Uri.UriSchemeHttps;
            }
            else if ("github".EqualsIgnoreCase(builder.Scheme))
            {
                repository.Type = "git";
                builder.Scheme = Uri.UriSchemeHttps;
                builder.Path = "github.com/" + builder.Path;
            }
            else if ("gitlab".EqualsIgnoreCase(builder.Scheme))
            {
                builder.Scheme = Uri.UriSchemeHttps;
                builder.Path = "gitlab.com/" + builder.Path;
            }
            else if ("bitbucket".EqualsIgnoreCase(builder.Scheme))
            {
                builder.Scheme = Uri.UriSchemeHttps;
                builder.Path = "bitbucket.org/" + builder.Path;
            }
            else if (!Uri.UriSchemeHttps.EqualsIgnoreCase(builder.Scheme) && !Uri.UriSchemeHttp.EqualsIgnoreCase(builder.Scheme))
            {
                return null;
            }

            repository.Url = builder.Uri.ToString();
            return repository;
        }

        public IEnumerable<NpmPackageId> GetDependencies() => ParseDependencies((JObject)Content.GetValue("dependencies"));

        public IEnumerable<NpmPackageId> GetDevDependencies() => ParseDependencies((JObject)Content.GetValue("devDependencies"));

        private static IEnumerable<NpmPackageId> ParseDependencies(JObject root)
        {
            if (root == null)
            {
                yield break;
            }

            foreach (var property in root.Properties())
            {
                yield return new NpmPackageId(property.Name, (string)property.Value);
            }
        }

        private static string ParseAuthorName(object value)
        {
            if (value is JObject obj)
            {
                return obj.Value<string>("name");
            }

            if (value is JValue name)
            {
                return (string)name.Value;
            }

            return null;
        }

        private static PackageJsonRepository ParseRepository(object repository)
        {
            if (repository is JObject obj)
            {
                return new PackageJsonRepository
                {
                    Type = obj.Value<string>("type"),
                    Url = obj.Value<string>("url")
                };
            }

            if (repository is JValue url)
            {
                return new PackageJsonRepository
                {
                    Url = (string)url.Value
                };
            }

            return null;
        }
    }
}
