using System;
using System.Collections.Generic;

namespace TerraSim
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the number, truncated by the given bounds.
        /// </summary>
        /// <param name="number">A number to constrain.</param>
        /// <param name="min">Inclusive lower bound.</param>
        /// <param name="max">Inclusive upper bound.</param>
        /// <returns></returns>
        public static int Bound(this int number, int min, int max)
        {
            return Math.Min(max, Math.Max(min, number));
        }

        /// <summary>
        /// Syntactic sugar for calling string.Format(str, arguments)
        /// </summary>
        /// <param name="str">str</param>
        /// <param name="arguments">arguments</param>
        /// <returns>string.Format(str, arguments)</returns>
        public static string Form(this string str, params object[] arguments)
        {
            return string.Format(str, arguments);
        }

        /// <summary>
        /// Moves the items from the second list to the end of the first.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements of the input sequence.</typeparam>
        /// <param name="target">Target sequence. Items are moved here.</param>
        /// <param name="source">Source sequence. Items are removed from here.</param>
        /// <remarks>Bear in mind that this method modifies both its input sequences.</remarks>
        public static void Move<TSource>(this LinkedList<TSource> target,
            LinkedList<TSource> source)
        {
            if (target == source)
            {
                return;
            }
            LinkedListNode<TSource> item = null;
            while (source.Count != 0)
            {
                item = source.First;
                source.RemoveFirst();
                target.AddLast(item);
            }
        }

    }
}
