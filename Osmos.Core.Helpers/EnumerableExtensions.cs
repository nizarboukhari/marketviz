using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmos.Core.Helpers
{
    public static class EnumerableExtensions
    {
        public static T RandomElement<T>(this IEnumerable<T> array)
        {
            var rnd = new Random();
            return array.ElementAt(rnd.Next(array.Count()));
        }

        public static IEnumerable<T> Suffle<T>(this IEnumerable<T> array)
        {
            var rnd = new Random();
            var result = array.OrderBy(x => rnd.Next());
            return result;
        }

        public static IEnumerable<T> Subset<T>(this IEnumerable<T> array, bool shuffle = true, int min = -1, int max = -1)
        {
            var rnd = new Random();

            int _min = Math.Max(min, 0);
            int _max = 0 < max && max <= array.Count() ? max : array.Count();

            int take = rnd.Next(_min, _max);

            var result = shuffle ? array.Suffle().Take(take) : array.Take(take);
            return result;
        }
    }
}
