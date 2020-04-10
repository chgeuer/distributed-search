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
            IBusinessLogicFilterPredicate<TContext, TItem> predicate => items.Where(item => predicate.Matches(ctx, item)),
            IBusinessLogicFilterProjection<TContext, TItem> mapper => items.Select(item => mapper.Map(ctx, item)),
            IBusinessLogicOrderStep<TContext, TItem> orderer => orderer.Order(ctx, items),
            _ => throw new NotSupportedException(message: $"Unclear how to handle {step.GetType().FullName}"),
        };

        public static IObservable<TItem> ApplySteps<TContext, TItem>(this IObservable<TItem> items, TContext ctx, IEnumerable<IBusinessLogicStep<TContext, TItem>> steps)
                => steps.Aggregate(items, (items, step) => items.ApplyStep(ctx, step));

        public static IObservable<TItem> ApplyStep<TContext, TItem>(this IObservable<TItem> items, TContext ctx, IBusinessLogicStep<TContext, TItem> step) => step switch
        {
            IBusinessLogicFilterProjection<TContext, TItem> mapper => items.Select(item => mapper.Map(ctx, item)),
            IBusinessLogicFilterPredicate<TContext, TItem> predicate => items.Where(item => predicate.Matches(ctx, item)),
            _ => throw new NotSupportedException(message: $"Unclear how to handle {step.GetType().FullName}"),
        };
    }
}
