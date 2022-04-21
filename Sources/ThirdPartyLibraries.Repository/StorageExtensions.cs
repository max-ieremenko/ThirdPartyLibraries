using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    public static class StorageExtensions
    {
        internal const string ReadMeFileName = "readme.md";
        internal const string ReadMeTemplateFileName = "readme-template.txt";
        internal const string IndexFileName = "index.json";
        internal const string AppSettingsFileName = "appsettings.json";

        private const string LibraryReadMeTemplateFileName = "{0}-readme-template.txt";
        private const string ThirdPartyNoticesTemplateFileName = "third-party-notices-template.txt";

        public static async Task<LicenseIndexJson> ReadLicenseIndexJsonAsync(this IStorage storage, string licenseCode, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            var content = await storage.OpenLicenseFileReadAsync(licenseCode, IndexFileName, token).ConfigureAwait(false);
            using (content)
            {
                return content?.JsonDeserialize<LicenseIndexJson>();
            }
        }

        public static async Task CreateLicenseIndexJsonAsync(this IStorage storage, LicenseIndexJson model, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));
            model.AssertNotNull(nameof(model));

            var content = JsonSerialize(model);

            try
            {
                await storage.CreateLicenseFileAsync(model.Code, IndexFileName, content, token).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                throw new NotSupportedException("License cannot be updated.", ex);
            }
        }

        public static async Task<TModel> ReadLibraryIndexJsonAsync<TModel>(this IStorage storage, LibraryId id, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            var content = await storage.OpenLibraryFileReadAsync(id, IndexFileName, token).ConfigureAwait(false);
            using (content)
            {
                return content == null ? default : content.JsonDeserialize<TModel>();
            }
        }

        public static async ValueTask<bool> LibraryFileExistsAsync(this IStorage storage, LibraryId id, string fileName, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));
            fileName.AssertNotNull(nameof(fileName));

            var content = await storage.OpenLibraryFileReadAsync(id, fileName, token).ConfigureAwait(false);
            using (content)
            {
                return content != null;
            }
        }

        public static async Task WriteLibraryIndexJsonAsync<TModel>(this IStorage storage, LibraryId id, TModel model, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));
            model.AssertNotNull(nameof(model));

            var content = JsonSerialize(model);
            await storage.WriteLibraryFileAsync(id, IndexFileName, content, token).ConfigureAwait(false);
        }

        public static async Task WriteLibraryReadMeAsync(this IStorage storage, LibraryId id, object context, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));
            context.AssertNotNull(nameof(context));

            var templateFileName = LibraryReadMeTemplateFileName.FormatWith(id.SourceCode.ToLowerInvariant());
            var template = await GetOrCreateConfigurationTemplateAsync(
                    storage,
                    templateFileName,
                    () => DotLiquidTemplate.GetLibraryReadMeTemplate(id.SourceCode),
                    token)
                .ConfigureAwait(false);

            var readMe = DotLiquidTemplate.Render(template, context);
            await storage.WriteLibraryFileAsync(id, ReadMeFileName, readMe, token).ConfigureAwait(false);
        }

        public static async Task WriteRootReadMeAsync(this IStorage storage, RootReadMeContext context, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));
            context.AssertNotNull(nameof(context));

            var template = await GetOrCreateConfigurationTemplateAsync(
                    storage,
                    ReadMeTemplateFileName,
                    DotLiquidTemplate.GetRootReadMeTemplate,
                    token)
                .ConfigureAwait(false);

            var readMe = DotLiquidTemplate.Render(template, context);
            await storage.WriteRootFileAsync(ReadMeFileName, readMe, token).ConfigureAwait(false);
        }

        public static Task<string> GetOrCreateThirdPartyNoticesTemplateAsync(this IStorage storage, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            return GetOrCreateConfigurationTemplateAsync(
                storage,
                ThirdPartyNoticesTemplateFileName,
                DotLiquidTemplate.GetThirdPartyNoticesTemplate,
                token);
        }

        public static Task<Stream> GetOrCreateAppSettingsAsync(this IStorage storage, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            return GetOrCreateConfigurationFileAsync(
                storage,
                AppSettingsFileName,
                DotLiquidTemplate.GetAppSettingsTemplate,
                token);
        }

        private static async Task<Stream> GetOrCreateConfigurationFileAsync(IStorage storage, string fileName, Func<byte[]> getContent, CancellationToken token)
        {
            var stream = await storage.OpenConfigurationFileReadAsync(fileName, token).ConfigureAwait(false);
            if (stream != null)
            {
                return stream;
            }

            var content = getContent();
            await storage.CreateConfigurationFileAsync(fileName, content, token).ConfigureAwait(false);

            return new MemoryStream(content);
        }

        private static async Task<string> GetOrCreateConfigurationTemplateAsync(IStorage storage, string fileName, Func<byte[]> getContent, CancellationToken token)
        {
            using (var stream = await GetOrCreateConfigurationFileAsync(storage, fileName, getContent, token).ConfigureAwait(false))
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        private static byte[] JsonSerialize(object content)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, null, -1, true))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    writer.NewLine = "\r\n";

                    jsonWriter.Formatting = Formatting.Indented;
                    new JsonSerializer().Serialize(jsonWriter, content);
                }

                return stream.ToArray();
            }
        }
    }
}
