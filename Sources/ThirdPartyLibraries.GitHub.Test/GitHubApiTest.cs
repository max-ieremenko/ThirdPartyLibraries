﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.GitHub
{
    [TestFixture]
    public class GitHubApiTest
    {
        private GitHubApi _sut;
        private MockHttpMessageHandler _mockHttp;

        [SetUp]
        public void BeforeEachTest()
        {
            _mockHttp = new MockHttpMessageHandler();

            _sut = new GitHubApi(_mockHttp.ToHttpClient);
        }

        [Test]
        public async Task LoadLicense()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://api.github.com/repos/JamesNK/Newtonsoft.Json/license")
                .WithHeaders("Authorization", "Token tokenValue")
                .Respond(
                    MediaTypeNames.Application.Json,
                    TempFile.OpenResource(GetType(), "GitHubApiTest.License.Newtonsoft.Json.json"));

            var actual = await _sut.LoadLicenseAsync("https://github.com/JamesNK/Newtonsoft.Json", "tokenValue", CancellationToken.None);

            actual.HasValue.ShouldBeTrue();
            actual.Value.SpdxId.ShouldBe("MIT");
            actual.Value.SpdxIdHRef.ShouldBe("https://api.github.com/repos/JamesNK/Newtonsoft.Json/license");
            actual.Value.FileName.ShouldBe("LICENSE.md");
            actual.Value.FileContentHRef.ShouldBe("https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md");
            actual.Value.FileContent.AsText().ShouldContain("Copyright (c) 2007 James Newton-King");
        }

        [Test]
        public async Task LoadNoAssertionLicense()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://api.github.com/repos/shouldly/shouldly/license")
                .Respond(
                    MediaTypeNames.Application.Json,
                    TempFile.OpenResource(GetType(), "GitHubApiTest.License.Shouldly.Json.json"));

            var actual = await _sut.LoadLicenseAsync("https://github.com/shouldly/shouldly.git", null, CancellationToken.None);

            actual.HasValue.ShouldBeTrue();
            actual.Value.SpdxId.ShouldBeNull();
            actual.Value.SpdxIdHRef.ShouldBe("https://api.github.com/repos/shouldly/shouldly/license");
            actual.Value.FileName.ShouldBe("LICENSE.txt");
            actual.Value.FileContentHRef.ShouldBe("https://raw.githubusercontent.com/shouldly/shouldly/master/LICENSE.txt");
            actual.Value.FileContent.AsText().ShouldContain("Redistribution and use in source and binary forms");
        }

        [Test]
        public async Task LoadLicenseNotFound()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://api.github.com/repos/JamesNK/Newtonsoft.Json/license")
                .Respond(HttpStatusCode.NotFound);

            var actual = await _sut.LoadLicenseAsync("https://github.com/JamesNK/Newtonsoft.Json", null, CancellationToken.None);

            actual.HasValue.ShouldBeFalse();
        }

        [Test]
        public void LoadLicenseUnauthorized()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://api.github.com/repos/JamesNK/Newtonsoft.Json/license")
                .WithHeaders("Authorization", "Token tokenValue")
                .Respond(
                    HttpStatusCode.Unauthorized,
                    MediaTypeNames.Application.Json,
                    TempFile.OpenResource(GetType(), "GitHubApiTest.LoadLicenseUnauthorized.json"));

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.LoadLicenseAsync("https://github.com/JamesNK/Newtonsoft.Json", "tokenValue", CancellationToken.None));

            ex.Message.ShouldContain("token");
        }

        [Test]
        [TestCase("https://github.com/JamesNK/Newtonsoft.Json", "https://api.github.com/repos/JamesNK/Newtonsoft.Json/license")]
        [TestCase("https://github.com/shouldly/shouldly.git", "https://api.github.com/repos/shouldly/shouldly/license")]
        [TestCase("https://raw.githubusercontent.com/moq/moq4/master/License.txt", "https://api.github.com/repos/moq/moq4/license")]
        [TestCase("https://api.github.com/repos/JamesNK/Newtonsoft.Json/license", "https://api.github.com/repos/JamesNK/Newtonsoft.Json/license")]
        [TestCase("https://github.com/unitycontainer", null)]
        public void GetLicenseUrl(string url, string expected)
        {
            GitHubApi.GetLicenseUrl(new Uri(url)).ShouldBe(expected);
        }
    }
}
