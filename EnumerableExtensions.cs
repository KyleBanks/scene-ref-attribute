using System.Collections;

namespace KBCore.Refs
{
    public static class EnumerableExtensions
    {
        public static int CountEnumerable(this IEnumerable enumerable)
        {
            int count = 0;
            foreach (var item in enumerable)
            {
                count++;
            }
            return count;
        }
    }
}