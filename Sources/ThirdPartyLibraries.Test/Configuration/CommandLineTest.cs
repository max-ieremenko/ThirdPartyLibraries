using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Configuration;

[TestFixture]
public class CommandLineTest
{
    [Test]
    public void Parse()
    {
        var actual = CommandLine.Parse(
            "some command",
            "-option1",
            "option1 value",
            "-option2");

        actual.Command.ShouldBe("some command");
        actual.Options.Count.ShouldBe(2);
            
        actual.Options[0].Name.ShouldBe("option1");
        actual.Options[0].Value.ShouldBe("option1 value");

        actual.Options[1].Name.ShouldBe("option2");
        actual.Options[1].Value.ShouldBeNull();
    }

    [Test]
    public void ParseEmpty()
    {
        var actual = CommandLine.Parse();

        actual.Command.ShouldBeNull();
        actual.Options.Count.ShouldBe(0);
    }

    [Test]
    [TestCase("command", "+option")]
    [TestCase("command", "option")]
    [TestCase("command", "--option", "value1", "value2")]
    [TestCase("command1", "command2")]
    public void FailToParse(params string[] args)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CommandLine.Parse(args));

        ex!.Message.ShouldContain(args.Last());
    }
}