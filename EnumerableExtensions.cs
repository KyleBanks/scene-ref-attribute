using System.Collections;

namespace KBCore.Refs
{
    public static class EnumerableExtensions
    {
        public static bool HaveSameCount(this IEnumerable enumerable1, IEnumerable enumerable2)
        {
            int count1 = enumerable1.CountEnumerable();
            int count2 = enumerable2.CountEnumerable();

            return count1 == count2;
        }

        public static int CountEnumerable(this IEnumerable enumerable)
        {
            int count = 0;
            foreach (var item in enumerable)
            {
                count++;
            }
            return count;
        }

        public static bool Any(this IEnumerable enumerable)
        {
            return enumerable.GetEnumerator().MoveNext();
        }
    }
}