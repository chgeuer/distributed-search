namespace Mercury.Utils
{
    using System;
    using Mercury.Interfaces;

    public class GenericFilter<TContext, TItem> : IBusinessLogicFilterPredicate<TContext, TItem>
    {
        public Func<TContext, TItem, bool> Match { get; }

        public GenericFilter(Func<TContext, TItem, bool> match)
        {
            this.Match = match;
        }

        bool IBusinessLogicFilterPredicate<TContext, TItem>.Matches(TContext ctx, TItem item) => this.Match(ctx, item);
    }
}