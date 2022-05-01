using System;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Repository.Template;

[TestFixture]
public class ApplicationTest
{
    [Test]
    public void DoNotSerializeDefaultValues()
    {
        var app = new Application
        {
            Name = "app name",
            InternalOnly = false
        };

        var json = app.ToJsonString();
        Console.WriteLine(json);

        json.ShouldNotContain(nameof(Application.TargetFrameworks));
        json.ShouldNotContain(nameof(Application.Dependencies));
    }

    [Test]
    public void Serialize()
    {
        var app = new Application
        {
            Name = "app name",
            InternalOnly = false,
            TargetFrameworks = new[] { "f1" },
            Dependencies = { new LibraryDependency() }
        };

        var json = app.ToJsonString();
        Console.WriteLine(json);

        json.ShouldContain(nameof(Application.TargetFrameworks));
        json.ShouldContain(nameof(Application.Dependencies));
    }
}