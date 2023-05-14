using System.Collections.Generic;

namespace GraphNodeRelax
{
    static class IListExtensions
    {
        public static void AddUnique<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }
    }
}
