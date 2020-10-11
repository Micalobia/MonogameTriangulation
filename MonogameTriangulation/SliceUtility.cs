using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonogameTriangulation
{
    public static class SliceUtility
    {
        public static T[] Slice<T>(this T[] arr, int left, int right)
        {
            int diff = right - left;
            if (diff <= 0) return new T[0];
            T[] ret = new T[diff];
            for (int i = left, j = 0; i < right; ++i, ++j) ret[j] = arr[i];
            return ret;
        }
    }
}
