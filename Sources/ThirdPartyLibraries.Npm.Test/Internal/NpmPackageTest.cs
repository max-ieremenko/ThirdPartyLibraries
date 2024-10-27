using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

[TestFixture]
public class NpmPackageTest
{
    [Test]
    public async Task ExtractPackageJson()
    {
        byte[] jsonContent;
        using (var content = TempFile.OpenResource(GetType(), "NpmRegistryTest.TypesAngular.1.6.56.tgz"))
        {
            jsonContent = NpmPackage.ExtractPackageJson(await content.ToArrayAsync(CancellationToken.None).ConfigureAwait(false));
        }

        NpmPackageSpec.FromStream(new MemoryStream(jsonContent)).GetName().ShouldBe("@types/angular");
    }

    [Test]
    [TestCase("LICENSE", "Copyright (c) Microsoft Corporation")]
    [TestCase("license", "Copyright (c) Microsoft Corporation")]
    [TestCase("license.txt", null)]
    public async Task LoadFileContent(string fileName, string? expected)
    {
        byte[]? file;
        using (var package = TempFile.OpenResource(GetType(), "NpmRegistryTest.TypesAngular.1.6.56.tgz"))
        {
            var packageContent = await package.ToArrayAsync(CancellationToken.None).ConfigureAwait(false);
            file = NpmPackage.LoadFileContent(packageContent, fileName);
        }

        if (expected == null)
        {
            file.ShouldBeNull();
        }
        else
        {
            file.ShouldNotBeNull();
            file.AsText().ShouldContain(expected);
        }
    }

    [Test]
    [TestCase("^license$", "LICENSE")]
    [TestCase("lic", "LICENSE")]
    [TestCase("i", "index.d.ts", "jqlite.d.ts", "LICENSE")]
    public async Task FindFiles(string searchPattern, params string[] expected)
    {
        string[] actual;
        using (var package = TempFile.OpenResource(GetType(), "NpmRegistryTest.TypesAngular.1.6.56.tgz"))
        {
            actual = NpmPackage.FindFiles(await package.ToArrayAsync(CancellationToken.None).ConfigureAwait(false), searchPattern);
        }

        actual.ShouldBe(expected, ignoreOrder: true);
    }
}