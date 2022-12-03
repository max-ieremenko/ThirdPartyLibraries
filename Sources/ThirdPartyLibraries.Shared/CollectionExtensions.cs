using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ThirdPartyLibraries.Shared;

public static class CollectionExtensions
{
    public static bool IsNullOrEmpty<T>([CanBeNull] this ICollection<T> list)
    {
        return list == null || list.Count == 0;
    }

    public static void AddRange<T>([NotNull] this ICollection<T> list, [NotNull] IEnumerable<T> values)
    {
        list.AssertNotNull(nameof(list));
        values.AssertNotNull(nameof(values));

        if (list is List<T> l)
        {
            l.AddRange(values);
        }
        else
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
        }
    }

    public static void RemoveAll<T>([NotNull] this IList<T> list, [NotNull] Func<T, bool> predicate)
    {
        list.AssertNotNull(nameof(list));
        predicate.AssertNotNull(nameof(predicate));

        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
                i--;
            }
        }
    }

    public static int IndexOf<T>([NotNull] this IList<T> list, [NotNull] Func<T, bool> predicate)
    {
        list.AssertNotNull(nameof(list));
        predicate.AssertNotNull(nameof(predicate));

        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                return i;
            }
        }

        return -1;
    }
}