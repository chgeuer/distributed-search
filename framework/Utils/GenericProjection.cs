namespace Mercury.Utils
{
    using System;
    using Mercury.Interfaces;

    public class GenericProjection<TContext, TItem> : IBusinessLogicFilterProjection<TContext, TItem>
    {
        public Func<TContext, TItem, TItem> Map { get; }

        public GenericProjection(Func<TContext, TItem, TItem> map)
        {
            this.Map = map;
        }

        TItem IBusinessLogicFilterProjection<TContext, TItem>.Map(TContext ctx, TItem item) => this.Map(ctx, item);
    }
}