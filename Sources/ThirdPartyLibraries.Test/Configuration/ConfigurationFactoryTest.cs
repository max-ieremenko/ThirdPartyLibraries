using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Configuration.ConfigurationManagerTestDomain;
using ThirdPartyLibraries.GitHub.Configuration;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Configuration;

[TestFixture]
public class ConfigurationFactoryTest
{
    [Test]
    public async Task DefaultToken()
    {
        var section = await new SutBuilder()
            .BuildSectionAsync<GitHubConfiguration>(GitHubConfiguration.SectionName)
            .ConfigureAwait(false);

        section.PersonalAccessToken.ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task OverrideTokenBySecret()
    {
        var section = await new SutBuilder()
            .WithSecrets()
            .BuildSectionAsync<GitHubConfiguration>(GitHubConfiguration.SectionName)
            .ConfigureAwait(false);

        section.PersonalAccessToken.ShouldBe("secret-token");
    }

    [Test]
    public async Task OverrideTokenByEnvironmentVariable()
    {
        var section = await new SutBuilder()
            .WithSecrets()
            .WithEnvironmentVariable(CommandOptions.OptionGitHubToken, "environment-token")
            .BuildSectionAsync<GitHubConfiguration>(GitHubConfiguration.SectionName)
            .ConfigureAwait(false);

        section.PersonalAccessToken.ShouldBe("environment-token");
    }

    [Test]
    public async Task OverrideTokenByCommandLine()
    {
        var section = await new SutBuilder()
            .WithSecrets()
            .WithEnvironmentVariable(CommandOptions.OptionGitHubToken, "environment-token")
            .WithCommandLine(CommandOptions.OptionGitHubToken, "commandLine-token")
            .BuildSectionAsync<GitHubConfiguration>(GitHubConfiguration.SectionName)
            .ConfigureAwait(false);

        section.PersonalAccessToken.ShouldBe("commandLine-token");
    }

    private sealed class SutBuilder
    {
        private readonly Dictionary<string, string> _environmentVariables = new();
        private readonly Dictionary<string, string?> _commandLine = new();
        private bool _addSecrets;

        public SutBuilder WithSecrets()
        {
            _addSecrets = true;
            return this;
        }

        public SutBuilder WithEnvironmentVariable(string name, string value)
        {
            _environmentVariables.Add(CommandOptions.EnvironmentVariablePrefix + name, value);
            return this;
        }

        public SutBuilder WithCommandLine(string name, string value)
        {
            _commandLine.Add(name, value);
            return this;
        }

        public async Task<T> BuildSectionAsync<T>(string sectionName)
            where T : new()
        {
            var services = new ServiceCollection();

            var storage = new Mock<IStorage>(MockBehavior.Strict);
            storage
                .Setup(s => s.OpenConfigurationFileReadAsync("appsettings.json", default))
                .ReturnsAsync(TempFile.OpenResource(typeof(ConfigurationFactoryTest), "ConfigurationManagerTestDomain.appsettings.json"));

            var builder = await ConfigurationFactory.CreateBuilderAsync(services, storage.Object, default).ConfigureAwait(false);
            storage.VerifyAll();

            if (_addSecrets)
            {
                builder.AddJsonStream(TempFile.OpenResource(typeof(ConfigurationFactoryTest), "ConfigurationManagerTestDomain.secrets.json"));
            }

            var variablesSource = new Mock<IConfigurationSource>(MockBehavior.Strict);
            variablesSource
                .Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns(new MockEnvironmentVariablesConfigurationProvider(_environmentVariables));
            builder.Add(variablesSource.Object);

            builder.AddInMemoryCollection(_commandLine);

            var configuration = builder.Build();
            var result = new T();
            configuration.GetSection(sectionName).Bind(result);

            return result;
        }
    }
}