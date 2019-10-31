namespace ThirdPartyLibraries.Suite
{
    public readonly struct PackageNoticesApplication
    {
        public PackageNoticesApplication(string name, bool internalOnly)
        {
            Name = name;
            InternalOnly = internalOnly;
        }

        public string Name { get; }

        public bool InternalOnly { get; }
    }
}