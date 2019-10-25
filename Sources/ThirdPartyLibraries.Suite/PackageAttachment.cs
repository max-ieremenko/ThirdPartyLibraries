namespace ThirdPartyLibraries.Suite
{
    public sealed class PackageAttachment
    {
        public PackageAttachment()
        {
        }

        public PackageAttachment(string name, byte[] content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; set; }

        public byte[] Content { get; set; }
    }
}
