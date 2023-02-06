namespace Mercury.Utils;

using Mercury.Fundamentals;
using Mercury.Interfaces;
using Microsoft.FSharp.Core;
using System;
using static Mercury.Fundamentals.BusinessLogic;

public class GenericFilter<TBusinessData, TSearchRequest, TItem> : IBusinessLogicPredicate<TBusinessData, TSearchRequest, TItem>, IPredicate<TBusinessData, TSearchRequest, TItem>
{
    public Func<TBusinessData, TSearchRequest, TItem, bool> Match { get; }

    public GenericFilter(Func<TBusinessData, TSearchRequest, TItem, bool> match)
    {
        this.Match = match;
    }

    bool IBusinessLogicPredicate<TBusinessData, TSearchRequest, TItem>.Matches(TBusinessData businessData, TSearchRequest searchRequest, TItem item) => this.Match(businessData, searchRequest, item);

    FSharpFunc<TBusinessData, FSharpFunc<TSearchRequest, FSharpFunc<TItem, bool>>> IPredicate<TBusinessData, TSearchRequest, TItem>.Matches => this.Match.ToFSharpFunc();
}