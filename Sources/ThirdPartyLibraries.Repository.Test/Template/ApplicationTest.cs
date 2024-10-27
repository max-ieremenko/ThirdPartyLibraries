using System.Text.Json;
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

        var json = JsonSerializer.Serialize(app, DomainJsonSerializerContext.Default.Application);
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
            TargetFrameworks = ["f1"],
            Dependencies = [new LibraryDependency { Name = "name", Version = "version" }]
        };

        var json = JsonSerializer.Serialize(app, DomainJsonSerializerContext.Default.Application);
        Console.WriteLine(json);

        json.ShouldContain(nameof(Application.TargetFrameworks));
        json.ShouldContain(nameof(Application.Dependencies));
    }
}