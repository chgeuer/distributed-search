namespace Mercury.Utils
{
    using System;
    using Mercury.Fundamentals;
    using Mercury.Interfaces;
    using Microsoft.FSharp.Core;
    using static Mercury.Fundamentals.BusinessLogic;

    public class GenericFilter<TContext, TItem> : IBusinessLogicPredicate<TContext, TItem>, IPredicate<TContext, TItem>
    {
        public Func<TContext, TItem, bool> Match { get; }

        public GenericFilter(Func<TContext, TItem, bool> match)
        {
            this.Match = match;
        }

        bool IBusinessLogicPredicate<TContext, TItem>.Matches(TContext ctx, TItem item) => this.Match(ctx, item);

        FSharpFunc<TContext, FSharpFunc<TItem, bool>> IPredicate<TContext, TItem>.Matches => this.Match.ToFSharpFunc();
    }
}