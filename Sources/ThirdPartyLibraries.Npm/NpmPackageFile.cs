namespace ThirdPartyLibraries.Npm
{
    public readonly struct NpmPackageFile
    {
        public NpmPackageFile(string name, byte[] content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }

        public byte[] Content { get; }
    }
}