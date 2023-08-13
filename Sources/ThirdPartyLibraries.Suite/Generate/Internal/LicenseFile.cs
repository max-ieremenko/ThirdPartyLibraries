using System;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class LicenseFile : IEquatable<LicenseFile>
{
    public LicenseFile(string licenseCode, string fileName, ArrayHash hash)
    {
        LicenseCode = licenseCode;
        FileName = fileName;
        Hash = hash;
    }

    public string LicenseCode { get; }

    public string FileName { get; }
    
    public ArrayHash Hash { get; }

    public bool Equals(LicenseFile? other) => other != null && Hash.Equals(other.Hash);

    public override bool Equals(object obj) => Equals(obj as LicenseFile);

    public override int GetHashCode() => Hash.GetHashCode();

    public override string ToString() => FileName;
}