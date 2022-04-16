using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Configuration.ConfigurationManagerTestDomain;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GitHubAdapters;

namespace ThirdPartyLibraries.Configuration
{
    [TestFixture]
    public class ConfigurationManagerTest
    {
        [Test]
        public void DefaultToken()
        {
            var section = new SutBuilder()
                .Build()
                .GetSection<GitHubConfiguration>(KnownHosts.GitHub);

            section.PersonalAccessToken.ShouldBeNullOrEmpty();
        }

        [Test]
        public void OverrideTokenBySecret()
        {
            var section = new SutBuilder()
                .WithSecrets()
                .Build()
                .GetSection<GitHubConfiguration>(KnownHosts.GitHub);

            section.PersonalAccessToken.ShouldBe("secret-token");
        }

        [Test]
        public void OverrideTokenByEnvironmentVariable()
        {
            var section = new SutBuilder()
                .WithSecrets()
                .WithEnvironmentVariable(CommandFactory.OptionGitHubToken, "environment-token")
                .Build()
                .GetSection<GitHubConfiguration>(KnownHosts.GitHub);

            section.PersonalAccessToken.ShouldBe("environment-token");
        }

        [Test]
        public void OverrideTokenByCommandLine()
        {
            var section = new SutBuilder()
                .WithSecrets()
                .WithEnvironmentVariable(CommandFactory.OptionGitHubToken, "environment-token")
                .WithCommandLine(CommandFactory.OptionGitHubToken, "commandLine-token")
                .Build()
                .GetSection<GitHubConfiguration>(KnownHosts.GitHub);

            section.PersonalAccessToken.ShouldBe("commandLine-token");
        }

        private sealed class SutBuilder
        {
            private readonly Dictionary<string, string> _environmentVariables = new();
            private readonly Dictionary<string, string> _commandLine = new();
            private bool _addSecrets;

            public SutBuilder WithSecrets()
            {
                _addSecrets = true;
                return this;
            }

            public SutBuilder WithEnvironmentVariable(string name, string value)
            {
                _environmentVariables.Add(CommandFactory.EnvironmentVariablePrefix + name, value);
                return this;
            }

            public SutBuilder WithCommandLine(string name, string value)
            {
                _commandLine.Add(name, value);
                return this;
            }

            public ConfigurationManager Build()
            {
                var builder = new ConfigurationBuilder();
                builder.Sources.Clear();

                builder.AddJsonStream(TempFile.OpenResource(typeof(ConfigurationManagerTest), "ConfigurationManagerTestDomain.appsettings.json"));
                if (_addSecrets)
                {
                    builder.AddJsonStream(TempFile.OpenResource(typeof(ConfigurationManagerTest), "ConfigurationManagerTestDomain.secrets.json"));
                }

                var variablesSource = new Mock<IConfigurationSource>(MockBehavior.Strict);
                variablesSource
                    .Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                    .Returns(new MockEnvironmentVariablesConfigurationProvider(_environmentVariables));
                builder.Add(variablesSource.Object);

                builder.AddInMemoryCollection(_commandLine);

                return new ConfigurationManager(builder.Build());
            }
        }
    }
}
