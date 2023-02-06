namespace Mercury.Utils.Extensions;

using Mercury.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

public static class BusinessLogicExtensions
{
    /*
     * TODO Needs to return IObservable<Command<TItem>>
     **/
    public static IEnumerable<TItem> ApplySteps<TBusinessData, TSearchRequest, TItem>(this IEnumerable<TItem> items, TBusinessData businessData, TSearchRequest searchRequest, IEnumerable<IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>> steps)
        => steps.Aggregate(items, (items, step) => items.ApplyStep(businessData, searchRequest, step));

    public static IEnumerable<TItem> ApplyStep<TBusinessData, TSearchRequest, TItem>(this IEnumerable<TItem> items, TBusinessData businessData, TSearchRequest searchRequest, IBusinessLogicStep<TBusinessData, TSearchRequest, TItem> step) => step switch
    {
        IBusinessLogicPredicate<TBusinessData, TSearchRequest, TItem> predicate => items.Where(item => predicate.Matches(businessData, searchRequest, item)),
        IBusinessLogicProjection<TBusinessData, TSearchRequest, TItem> mapper => items.Select(item => mapper.Map(businessData, searchRequest, item)),
        IBusinessLogicEnumerableProcessor<TBusinessData, TSearchRequest, TItem> processor => processor.Process(businessData, searchRequest, items),
        IBusinessLogicObservableProcessor<TBusinessData, TSearchRequest, TItem> processor => processor.Stream(businessData, searchRequest, items.ToObservable()).ToEnumerable(),
        _ => throw new NotSupportedException(message: $"Unclear how to handle {step.GetType().FullName}"),
    };

    public static IObservable<TItem> ApplySteps<TBusinessData, TSearchRequest, TItem>(this IObservable<TItem> items, TBusinessData businessData, TSearchRequest searchRequest, IEnumerable<IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>> steps)
            => steps.Aggregate(items, (items, step) => items.ApplyStep(businessData, searchRequest, step));

    public static IObservable<TItem> ApplyStep<TBusinessData, TSearchRequest, TItem>(this IObservable<TItem> items, TBusinessData businessData, TSearchRequest searchRequest, IBusinessLogicStep<TBusinessData, TSearchRequest, TItem> step) => step switch
    {
        IBusinessLogicProjection<TBusinessData, TSearchRequest, TItem> mapper => items.Select(item => mapper.Map(businessData, searchRequest, item)),
        IBusinessLogicPredicate<TBusinessData, TSearchRequest, TItem> predicate => items.Where(item => predicate.Matches(businessData, searchRequest, item)),
        IBusinessLogicObservableProcessor<TBusinessData, TSearchRequest, TItem> processor => processor.Stream(businessData, searchRequest, items),
        IBusinessLogicEnumerableProcessor<TBusinessData, TSearchRequest, TItem> processor => processor.Process(businessData, searchRequest, items.ToEnumerable()).ToObservable(),
        _ => throw new NotSupportedException(message: $"Unclear how to handle {step.GetType().FullName}"),
    };
}
