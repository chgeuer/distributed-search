namespace Mercury.Utils.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.FSharp.Collections;

    public static class LinqExtensions
    {
        public static async Task ForeachAwaiting<T>(this IEnumerable<T> values, Func<T, Task> action)
        {
            foreach (var value in values)
            {
                await action(value);
            }
        }

        public static void Foreach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
            {
                action(value);
            }
        }

        public static void Each<T>(this IEnumerable<T> values, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in values)
            {
                action(e, i);
                i += 1;
            }
        }

        public static FSharpMap<TK, TV> ToFSharpMap<TK, TV>(
            this IEnumerable<ValueTuple<TK, TV>> source) =>
                new FSharpMap<TK, TV>(
                    source.Select(t => t.ToTuple()));

        public static FSharpMap<TK1, FSharpMap<TK2, TV>> ToFSharpMapOfMaps<TK1, TK2, TV>(
            this ValueTuple<TK1, ValueTuple<TK2, TV>[]>[] source) =>
                new FSharpMap<TK1, FSharpMap<TK2, TV>>(
                    source.Select(x => new Tuple<TK1, FSharpMap<TK2, TV>>(
                        x.Item1, x.Item2.ToFSharpMap())));
    }
}