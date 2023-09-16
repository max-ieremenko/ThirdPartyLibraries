using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository;

public static class StorageExtensions
{
    public const string ThirdPartyNoticesTemplateFileName = "third-party-notices-template.txt";
        
    internal const string ReadMeFileName = "readme.md";
    internal const string ReadMeTemplateFileName = "readme-template.txt";
    internal const string IndexFileName = "index.json";
    internal const string AppSettingsFileName = "appsettings.json";
    internal const string RemarksFileName = "remarks.md";
    internal const string ThirdPartyNoticesFileName = "third-party-notices.txt";

    private const string LibraryReadMeTemplateFileName = "{0}-readme-template.txt";

    public static async Task<LicenseIndexJson?> ReadLicenseIndexJsonAsync(this IStorage storage, string licenseCode, CancellationToken token)
    {
        var content = await storage.OpenLicenseFileReadAsync(licenseCode, IndexFileName, token).ConfigureAwait(false);
        try
        {
            return content?.JsonDeserialize<LicenseIndexJson>();
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize contents of {IndexFileName} of license {licenseCode}.",
                ex);
        }
        finally
        {
            content?.Dispose();
        }
    }

    public static async Task CreateLicenseIndexJsonAsync(this IStorage storage, LicenseIndexJson model, CancellationToken token)
    {
        var content = JsonSerialize(model);

        try
        {
            await storage.CreateLicenseFileAsync(model.Code, IndexFileName, content, token).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new NotSupportedException($"License {model.Code} cannot be updated.", ex);
        }
    }

    public static async Task<TModel?> ReadLibraryIndexJsonAsync<TModel>(this IStorage storage, LibraryId id, CancellationToken token)
    {
        var content = await storage.OpenLibraryFileReadAsync(id, IndexFileName, token).ConfigureAwait(false);
        try
        {
            return content == null ? default : content.JsonDeserialize<TModel>();
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize contents of {IndexFileName} of package {id.SourceCode}/{id.Name}/{id.Version}.",
                ex);
        }
        finally
        {
            content?.Dispose();
        }
    }

    public static async ValueTask<bool> LibraryFileExistsAsync(this IStorage storage, LibraryId id, string fileName, CancellationToken token)
    {
        var content = await storage.OpenLibraryFileReadAsync(id, fileName, token).ConfigureAwait(false);
        using (content)
        {
            return content != null;
        }
    }

    public static async Task WriteLibraryIndexJsonAsync<TModel>(this IStorage storage, LibraryId id, TModel model, CancellationToken token)
    {
        var content = JsonSerialize(model!);
        await storage.WriteLibraryFileAsync(id, IndexFileName, content, token).ConfigureAwait(false);
    }

    public static async Task WriteLibraryReadMeAsync(this IStorage storage, LibraryId id, object context, CancellationToken token)
    {
        var templateFileName = string.Format(LibraryReadMeTemplateFileName, id.SourceCode.ToLowerInvariant());
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
        return GetOrCreateConfigurationTemplateAsync(
            storage,
            ThirdPartyNoticesTemplateFileName,
            DotLiquidTemplate.GetThirdPartyNoticesTemplate,
            token);
    }

    public static async Task<MemoryStream> GetOrCreateAppSettingsAsync(this IStorage storage, CancellationToken token)
    {
        using var content = await GetOrCreateConfigurationFileAsync(
                storage,
                AppSettingsFileName,
                DotLiquidTemplate.GetAppSettingsTemplate,
                token)
            .ConfigureAwait(false);
        
        var result = new MemoryStream();
        await content.CopyToAsync(result, token).ConfigureAwait(false);

        result.Seek(0, SeekOrigin.Begin);
        return result;
    }

    public static Task<string?> ReadThirdPartyNoticesFileAsync(this IStorage storage, LibraryId id, CancellationToken token)
    {
        return ReadLibraryTextFileAsync(storage, id, ThirdPartyNoticesFileName, token);
    }

    public static Task<string?> ReadRemarksFileAsync(this IStorage storage, LibraryId id, CancellationToken token)
    {
        return ReadLibraryTextFileAsync(storage, id, RemarksFileName, token);
    }

    public static Task CreateDefaultRemarksFileAsync(this IStorage storage, LibraryId id, CancellationToken token)
    {
        return CreateLibraryEmptyFileAsync(storage, id, RemarksFileName, token);
    }

    public static Task CreateDefaultThirdPartyNoticesFileAsync(this IStorage storage, LibraryId id, CancellationToken token)
    {
        return CreateLibraryEmptyFileAsync(storage, id, ThirdPartyNoticesFileName, token);
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

    private static async Task<string?> ReadLibraryTextFileAsync(IStorage storage, LibraryId id, string fileName, CancellationToken token)
    {
        string? result = null;
        using (var stream = await storage.OpenLibraryFileReadAsync(id, fileName, token).ConfigureAwait(false))
        {
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    result = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static async Task CreateLibraryEmptyFileAsync(IStorage storage, LibraryId id, string fileName, CancellationToken token)
    {
        var exists = await LibraryFileExistsAsync(storage, id, fileName, token).ConfigureAwait(false);
        if (!exists)
        {
            await storage.WriteLibraryFileAsync(id, fileName, Array.Empty<byte>(), token).ConfigureAwait(false);
        }
    }
}