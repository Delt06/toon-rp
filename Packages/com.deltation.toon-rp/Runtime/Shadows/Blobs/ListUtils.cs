using System.Collections.Generic;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static class ListUtils
    {
        public static void FastRemoveByValue<T>(this List<T> list, T value) where T : class
        {
            int index = -1;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return;
            }

            int lastIndex = list.Count - 1;
            if (index == lastIndex)
            {
                list.RemoveAt(index);
            }
            else
            {
                T lastItem = list[lastIndex];
                list[index] = lastItem;
                list.RemoveAt(lastIndex);
            }
        }
    }
}