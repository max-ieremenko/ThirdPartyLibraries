using System;
using System.Globalization;
using System.IO;
using DotLiquid;
using DotLiquid.NamingConventions;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Repository;

public static class DotLiquidTemplate
{
    static DotLiquidTemplate()
    {
        DotLiquid.Template.NamingConvention = new CSharpNamingConvention();

        var allowAllMembers = new[] { "*" };

        DotLiquid.Template.RegisterSafeType(typeof(RootReadMeContext), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(RootReadMeLicenseContext), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(RootReadMePackageContext), allowAllMembers);

        DotLiquid.Template.RegisterSafeType(typeof(LibraryReadMeContext), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(LibraryLicense), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(LibraryReadMeDependencyContext), allowAllMembers);

        DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesContext), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesLicenseContext), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesPackageContext), allowAllMembers);
        DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesPackageLicenseContext), allowAllMembers);
    }

    public static void RenderTo(Stream stream, string templateSource, object context)
    {
        var template = DotLiquid.Template.Parse(templateSource);
        var templateParameters = new RenderParameters(CultureInfo.InvariantCulture)
        {
            LocalVariables = Hash.FromAnonymousObject(context)
        };

        using (var writer = new StreamWriter(stream, null, -1, true))
        {
            template.Render(writer, templateParameters);
        }
    }

    internal static byte[] GetRootReadMeTemplate() => GetTemplate("Root.ReadMeTemplate.txt");

    internal static byte[] GetLibraryReadMeTemplate(string librarySourceCode) => GetTemplate(librarySourceCode + ".ReadMeTemplate.txt");

    internal static byte[] GetThirdPartyNoticesTemplate() => GetTemplate("ThirdPartyNotices.Template.txt");

    internal static byte[] GetAppSettingsTemplate() => GetTemplate("appsettings.json");

    internal static byte[] Render(string templateSource, object context)
    {
        using (var stream = new MemoryStream())
        {
            RenderTo(stream, templateSource, context);
            return stream.ToArray();
        }
    }

    private static byte[] GetTemplate(string fileName)
    {
        var anchor = typeof(RootReadMeContext);

        using (var source = anchor.Assembly.GetManifestResourceStream(anchor, fileName))
        {
            if (source == null)
            {
                throw new ArgumentOutOfRangeException(nameof(fileName), $"Template [{fileName}] not found.");
            }

            using (var dest = new MemoryStream())
            {
                source.CopyTo(dest);
                return dest.ToArray();
            }
        }
    }
}