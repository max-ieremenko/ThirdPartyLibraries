using System;

namespace ThirdPartyLibraries
{
    public static class CollectionExtensions
    {
        public static int FindIndex<T>(this ReadOnlySpan<T> collection, Func<T, bool> predicate)
        {
            var index = 0;
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static int FindLastIndex<T>(this ReadOnlySpan<T> collection, Func<T, bool> predicate)
        {
            var index = 0;
            var result = -1;
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    result = index;
                }

                index++;
            }

            return result;
        }
    }
}
