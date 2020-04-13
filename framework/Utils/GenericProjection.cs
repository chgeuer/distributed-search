namespace Mercury.Utils
{
    using System;
    using Mercury.Fundamentals;
    using Mercury.Interfaces;
    using Microsoft.FSharp.Core;
    using static Mercury.Fundamentals.BusinessLogic;

    public class GenericProjection<TContext, TItem> : IBusinessLogicProjection<TContext, TItem>, IProjection<TContext, TItem>
    {
        public Func<TContext, TItem, TItem> Map { get; }

        public GenericProjection(Func<TContext, TItem, TItem> map)
        {
            this.Map = map;
        }

        TItem IBusinessLogicProjection<TContext, TItem>.Map(TContext ctx, TItem item) => this.Map(ctx, item);

        FSharpFunc<TContext, FSharpFunc<TItem, TItem>> IProjection<TContext, TItem>.Map => this.Map.ToFSharpFunc();
    }
}