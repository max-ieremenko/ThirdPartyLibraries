using System.Globalization;
using System.IO;
using DotLiquid;
using DotLiquid.NamingConventions;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    public static class DotLiquidTemplate
    {
        static DotLiquidTemplate()
        {
            DotLiquid.Template.NamingConvention = new CSharpNamingConvention();

            DotLiquid.Template.RegisterSafeType(typeof(RootReadMeContext), new[] { "*" });
            DotLiquid.Template.RegisterSafeType(typeof(RootReadMeLicenseContext), new[] { "*" });
            DotLiquid.Template.RegisterSafeType(typeof(RootReadMePackageContext), new[] { "*" });

            DotLiquid.Template.RegisterSafeType(typeof(NuGetReadMeContext), new[] { "*" });
            DotLiquid.Template.RegisterSafeType(typeof(LibraryLicense), new[] { "*" });
            DotLiquid.Template.RegisterSafeType(typeof(NuGetReadMeDependencyContext), new[] { "*" });

            DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesContext), new[] { "*" });
            DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesLicenseContext), new[] { "*" });
            DotLiquid.Template.RegisterSafeType(typeof(ThirdPartyNoticesPackageContext), new[] { "*" });
        }

        public static void RenderTo(Stream stream, string templateSource, object context)
        {
            stream.AssertNotNull(nameof(stream));
            templateSource.AssertNotNull(nameof(templateSource));
            context.AssertNotNull(nameof(context));

            var template = DotLiquid.Template.Parse(templateSource);
            var templateParameters = new RenderParameters(CultureInfo.InvariantCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(context)
            };

            using (var writer = new StreamWriter(stream, leaveOpen: true))
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
            using (var dest = new MemoryStream())
            {
                source.CopyTo(dest);
                return dest.ToArray();
            }
        }
    }
}
