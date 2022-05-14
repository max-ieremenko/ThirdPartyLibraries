using System.Collections.Generic;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

internal sealed class NamesGroup
{
    // preferredFileName must be unique in all groups
    public NamesGroup(string groupName, byte[] groupHash, string fileExtension, string preferredFileName)
    {
        GroupName = groupName;
        GroupHash = groupHash;
        FileExtension = fileExtension;
        PreferredFileName = preferredFileName;
        Names = new SortedSet<Name>();
        AlternativeNames = new SortedSet<Name>();
    }

    public string GroupName { get; }

    public byte[] GroupHash { get; }

    public string FileExtension { get; }

    public string PreferredFileName { get; }

    public SortedSet<Name> Names { get; }

    public SortedSet<Name> AlternativeNames { get; }

    public override string ToString() => "{0} {1} names".FormatWith(GroupName, Names.Count);
}