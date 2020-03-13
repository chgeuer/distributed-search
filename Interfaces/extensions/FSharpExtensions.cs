namespace Interfaces
{
    using System.Collections.Generic;
    using Microsoft.FSharp.Collections;

    public static class FSharpExtensions
    {
        public static FSharpList<T> ToFSharp<T>(this IEnumerable<T> source) => ListModule.OfSeq(source);

        // static FSharpFunc<K, V> ToFSharp<K,V>(Func<K, V> func) => FSharpFunc<K, V>.FromConverter(new Converter<K, V>(func));
    }
}