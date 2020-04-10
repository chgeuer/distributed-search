namespace Mercury.Utils.Extensions
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    /// <summary>
    /// Supports the dummy provider.
    /// </summary>
    public static class ReactiveExtensionExtensions
    {
        public static IObservable<T> EmitIn<T>(this T t, TimeSpan dueTime)
            => Observable.Timer(dueTime: dueTime).Select(_ => t);

        public static (IObservable<T>, T) And<T>(this IObservable<T> sequence, T t)
            => (sequence, t);

        public static IObservable<T> In<T>(this ValueTuple<IObservable<T>, T> tup, TimeSpan dueTime)
            => tup.Item1.Merge(tup.Item2.EmitIn(dueTime));

        private static IObservable<int> Demo() =>
           1.EmitIn(TimeSpan.FromSeconds(1))
           .And(2).In(TimeSpan.FromSeconds(2));
    }
}