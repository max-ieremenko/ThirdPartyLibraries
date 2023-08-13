using System.Collections.Generic;
using System.Linq;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

internal sealed class SourceCodeParser : ISourceCodeParser
{
    private readonly IEnumerable<IPackageReferenceProvider> _providers;

    public SourceCodeParser(IEnumerable<IPackageReferenceProvider> providers)
    {
        _providers = providers;
    }

    public List<IPackageReference> GetReferences(IList<string> locations)
    {
        var references = new List<IPackageReference>();
        var notFound = new HashSet<LibraryId>();

        foreach (var provider in _providers)
        {
            foreach (var location in locations)
            {
                provider.AddReferencesFrom(location, references, notFound);
            }
        }

        if (notFound.Count > 0)
        {
            throw new ReferenceNotFoundException(notFound.ToArray());
        }

        var result = Distinct(references);
        result.Sort(CompareReference);
        return result;
    }

    internal static List<IPackageReference> Distinct(List<IPackageReference> references)
    {
        var result = new Dictionary<LibraryId, IPackageReference>(references.Count);

        for (var i = 0; i < references.Count; i++)
        {
            var reference = references[i];

            var key = reference.Id;
            if (!result.TryGetValue(key, out var existing))
            {
                result.Add(key, reference);
            }
            else
            {
                result[key] = existing.UnionWith(reference);
            }
        }

        return new List<IPackageReference>(result.Values);
    }

    private static int CompareReference(IPackageReference x, IPackageReference y) => x.Id.CompareTo(y.Id);
}