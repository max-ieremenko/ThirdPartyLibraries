using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetPackageReference : IPackageReference
{
    public NuGetPackageReference(
        LibraryId id,
        string[] targetFrameworks,
        List<LibraryId> dependencies,
        bool isInternal,
        List<Uri> sources)
    {
        Id = id;
        TargetFrameworks = targetFrameworks;
        Dependencies = dependencies;
        IsInternal = isInternal;
        Sources = sources;
    }

    public LibraryId Id { get; }

    public string[] TargetFrameworks { get; }

    public List<LibraryId> Dependencies { get; }

    public bool IsInternal { get; }

    public List<Uri> Sources { get; }

    public IPackageReference UnionWith(IPackageReference other)
    {
        if (other is not NuGetPackageReference otherReference || !other.Id.Equals(Id))
        {
            throw new ArgumentOutOfRangeException(nameof(other));
        }

        var shouldCombineFrameworks = ShouldCombine(TargetFrameworks, other.TargetFrameworks);
        var shouldCombineInternal = IsInternal != other.IsInternal;

        if (shouldCombineFrameworks || shouldCombineInternal)
        {
            var targetFrameworks = shouldCombineFrameworks ? TargetFrameworks.Union(other.TargetFrameworks, StringComparer.OrdinalIgnoreCase).ToArray() : TargetFrameworks;
            var isInternal = IsInternal && other.IsInternal;

            return new NuGetPackageReference(
                Id,
                targetFrameworks,
                Dependencies.Union(other.Dependencies).ToList(),
                isInternal,
                Sources.Union(otherReference.Sources).ToList());
        }

        return this;
    }

    public override string ToString() => Id.ToString();

    private static bool ShouldCombine(string[] superSet, string[] subSet)
    {
        if (subSet.Length > superSet.Length)
        {
            return true;
        }

        for (var i = 0; i < subSet.Length; i++)
        {
            var flag = superSet.Contains(subSet[i], StringComparer.OrdinalIgnoreCase);
            if (!flag)
            {
                return true;
            }
        }

        return false;
    }
}