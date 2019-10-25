using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GitHubAdapters
{
    [TestFixture]
    public class GitHubConfigurationTest
    {
        private const string EnvironmentPrefix = "GitHubConfigurationTest_";
        private GitHubConfiguration _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _sut = new GitHubConfiguration();
        }

        [Test]
        public void Bind()
        {
            LoadConfiguration().GetSection(KnownHosts.GitHub).Bind(_sut);

            _sut.PersonalAccessToken.ShouldBe("value");
        }

        [Test]
        public void BindEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable(EnvironmentPrefix + "github.com:personalAccessToken", "env value", EnvironmentVariableTarget.Process);

            LoadConfiguration().GetSection(KnownHosts.GitHub).Bind(_sut);

            _sut.PersonalAccessToken.ShouldBe("env value");
        }

        private IConfigurationRoot LoadConfiguration()
        {
            using var file = TempFile.FromResource(GetType(), "GitHubConfigurationTest.appsettings.json");

            return new ConfigurationBuilder()
                .AddJsonFile(file.Location, false, false)
                .AddEnvironmentVariables(prefix: EnvironmentPrefix)
                .Build();
        }
    }
}
