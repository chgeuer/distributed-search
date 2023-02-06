namespace Mercury.Utils;

using Mercury.Fundamentals;
using Mercury.Interfaces;
using Microsoft.FSharp.Core;
using System;
using static Mercury.Fundamentals.BusinessLogic;

public class GenericProjection<TBusinessData, TSearchRequest, TItem> : IBusinessLogicProjection<TBusinessData, TSearchRequest, TItem>, IProjection<TBusinessData, TSearchRequest, TItem>
{
    public Func<TBusinessData, TSearchRequest, TItem, TItem> Map { get; }

    public GenericProjection(Func<TBusinessData, TSearchRequest, TItem, TItem> map)
    {
        this.Map = map;
    }

    TItem IBusinessLogicProjection<TBusinessData, TSearchRequest, TItem>.Map(TBusinessData businessData, TSearchRequest searchRequest, TItem item) => this.Map(businessData, searchRequest, item);

    FSharpFunc<TBusinessData, FSharpFunc<TSearchRequest, FSharpFunc<TItem, TItem>>> IProjection<TBusinessData, TSearchRequest, TItem>.Map => this.Map.ToFSharpFunc();
}