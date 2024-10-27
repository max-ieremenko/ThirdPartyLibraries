namespace ThirdPartyLibraries.GitHub.Internal;

internal sealed class DummyResponse
{
    public const string Uri = "https://github.com/dummy";
    public const string Content = "{ \"foo\":1 }";

    public int Foo { get; set; }
}