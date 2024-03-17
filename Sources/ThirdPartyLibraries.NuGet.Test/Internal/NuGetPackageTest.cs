using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class NuGetPackageTest
{
    [Test]
    public async Task ExtractSpecAsync()
    {
        var packageContent = await GetStyleCopAnalyzersAsync().ConfigureAwait(false);

        var actual = await NuGetPackage.ExtractSpecAsync("StyleCop.Analyzers", packageContent, default).ConfigureAwait(false);

        NuGetPackageSpec.FromStream(new MemoryStream(actual)).GetName().ShouldBe("StyleCop.Analyzers");
    }

    [Test]
    [TestCase("LICENSE", "Copyright (c) Tunnel Vision Laboratories")]
    [TestCase("license", "Copyright (c) Tunnel Vision Laboratories")]
    [TestCase("LICENSE.txt", null)]
    [TestCase("tools/install.ps1", "param($installPath, $toolsPath, $package, $project)")]
    [TestCase("tools\\install.ps1", "param($installPath, $toolsPath, $package, $project)")]
    public async Task LoadFileContentStyleCopAnalyzers(string fileName, string? expected)
    {
        var packageContent = await GetStyleCopAnalyzersAsync().ConfigureAwait(false);

        var actual = await NuGetPackage.LoadFileContentAsync(packageContent, fileName, default).ConfigureAwait(false);

        if (expected == null)
        {
            actual.ShouldBeNull();
        }
        else
        {
            actual.ShouldNotBeNull();
            actual.AsText().ShouldContain(expected);
        }
    }

    [Test]
    [TestCase("^license$", "LICENSE")]
    [TestCase("lic", "LICENSE")]
    [TestCase("install")]
    public async Task FindFiles(string searchPattern, params string[] expected)
    {
        var packageContent = await GetStyleCopAnalyzersAsync().ConfigureAwait(false);

        var actual = NuGetPackage.FindFiles(packageContent, searchPattern);

        actual.ShouldBe(expected, ignoreOrder: true);
    }

    private static async Task<byte[]> GetStyleCopAnalyzersAsync()
    {
        using (var content = TempFile.OpenResource(typeof(NuGetPackageTest), "NuGetPackageTest.StyleCop.Analyzers.1.1.118.nupkg"))
        {
            return await content.ToArrayAsync(default).ConfigureAwait(false);
        }
    }
}