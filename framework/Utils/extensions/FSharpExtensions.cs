namespace Mercury.Utils.Extensions
{
    using System;
    using System.Collections.Generic;
    using Microsoft.FSharp.Collections;
    using Microsoft.FSharp.Core;

    public static class FSharpExtensions
    {
        public static FSharpList<T> ToFSharp<T>(this IEnumerable<T> source) => ListModule.OfSeq(source);

        public static FSharpFunc<TInput, TResult> ToFSharp<TInput, TResult>(Func<TInput, TResult> func) => FSharpFunc<TInput, TResult>.FromConverter(new Converter<TInput, TResult>(func));

        public static bool OptionEqualsValue<T>(this FSharpOption<T> tOption, T tValue)
            => FSharpOption<T>.get_IsSome(tOption) && tOption.Value.Equals(tValue);

        public static FSharpFunc<TInput, TResult> ToFSharpFunc<TInput, TResult>(Func<TInput, TResult> func)
            => FSharpFunc<TInput, TResult>.FromConverter(
                new Converter<TInput, TResult>(func));

        public static FSharpFunc<Tuple<T1, T2>, TResult> ToFSharpFunc<T1, T2, TResult>(this Func<T1, T2, TResult> func)
            => FSharpFunc<Tuple<T1, T2>, TResult>.FromConverter(
                new Converter<Tuple<T1, T2>, TResult>(
                    t => func(t.Item1, t.Item2)));
    }
}