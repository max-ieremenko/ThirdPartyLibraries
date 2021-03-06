﻿using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class SourceCodeParser : ISourceCodeParser
    {
        public SourceCodeParser(IUnityContainer container)
        {
            container.AssertNotNull(nameof(container));

            Container = container;
        }

        public IUnityContainer Container { get; }

        public IList<LibraryReference> GetReferences(IList<string> locations)
        {
            locations.AssertNotNull(nameof(locations));

            var references = new List<LibraryReference>();

            var parsers = Container.ResolveAll<ISourceCodeReferenceProvider>();
            foreach (var parser in parsers)
            {
                foreach (var location in locations)
                {
                    references.AddRange(parser.GetReferencesFrom(location));
                }
            }

            return Distinct(references);
        }

        public IList<LibraryReference> Distinct(IEnumerable<LibraryReference> references)
        {
            references.AssertNotNull(nameof(references));

            var result = new Dictionary<LibraryId, LibraryReference>();
            foreach (var reference in references)
            {
                var key = reference.Id;
                if (!result.TryGetValue(key, out var existing))
                {
                    result.Add(key, reference);
                }
                else
                {
                    var shouldCombineFrameworks = ShouldCombine(existing.TargetFrameworks, reference.TargetFrameworks);
                    var shouldCombineInternal = existing.IsInternal != reference.IsInternal;

                    if (shouldCombineFrameworks || shouldCombineInternal)
                    {
                        var targetFrameworks = shouldCombineFrameworks ? existing.TargetFrameworks.Union(reference.TargetFrameworks, StringComparer.OrdinalIgnoreCase).ToArray() : existing.TargetFrameworks;
                        var isInternal = existing.IsInternal && reference.IsInternal;

                        result[key] = new LibraryReference(
                            key,
                            targetFrameworks,
                            existing.Dependencies.Union(reference.Dependencies).ToArray(),
                            isInternal);
                    }
                }
            }

            return new List<LibraryReference>(result.Values);
        }

        private static bool ShouldCombine(IList<string> superSet, IList<string> subSet)
        {
            if (subSet.Count > superSet.Count)
            {
                return true;
            }

            for (var i = 0; i < subSet.Count; i++)
            {
                var flag = superSet.Contains(subSet[i]);
                if (!flag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
