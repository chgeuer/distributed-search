namespace Mercury.Utils.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Mercury.Interfaces;

    public static class BusinessLogicExtensions
    {
        /*
         * TODO Needs to return IObservable<Command<TItem>>
         **/
        public static IEnumerable<TItem> ApplySteps<TContext, TItem>(this IEnumerable<TItem> items, TContext ctx, IEnumerable<IBusinessLogicStep<TContext, TItem>> steps)
            => steps.Aggregate(items, (items, step) => items.ApplyStep(ctx, step));

        public static IEnumerable<TItem> ApplyStep<TContext, TItem>(this IEnumerable<TItem> items, TContext ctx, IBusinessLogicStep<TContext, TItem> step) => step switch
        {
            IBusinessLogicPredicate<TContext, TItem> predicate => items.Where(item => predicate.Matches(ctx, item)),
            IBusinessLogicProjection<TContext, TItem> mapper => items.Select(item => mapper.Map(ctx, item)),
            IBusinessLogicEnumerableProcessor<TContext, TItem> processor => processor.Process(ctx, items),
            IBusinessLogicObservableProcessor<TContext, TItem> processor => processor.Stream(ctx, items.ToObservable()).ToEnumerable(),
            _ => throw new NotSupportedException(message: $"Unclear how to handle {step.GetType().FullName}"),
        };

        public static IObservable<TItem> ApplySteps<TContext, TItem>(this IObservable<TItem> items, TContext ctx, IEnumerable<IBusinessLogicStep<TContext, TItem>> steps)
                => steps.Aggregate(items, (items, step) => items.ApplyStep(ctx, step));

        public static IObservable<TItem> ApplyStep<TContext, TItem>(this IObservable<TItem> items, TContext ctx, IBusinessLogicStep<TContext, TItem> step) => step switch
        {
            IBusinessLogicProjection<TContext, TItem> mapper => items.Select(item => mapper.Map(ctx, item)),
            IBusinessLogicPredicate<TContext, TItem> predicate => items.Where(item => predicate.Matches(ctx, item)),
            IBusinessLogicObservableProcessor<TContext, TItem> processor => processor.Stream(ctx, items),
            IBusinessLogicEnumerableProcessor<TContext, TItem> processor => processor.Process(ctx, items.ToEnumerable()).ToObservable(),
            _ => throw new NotSupportedException(message: $"Unclear how to handle {step.GetType().FullName}"),
        };
    }
}
