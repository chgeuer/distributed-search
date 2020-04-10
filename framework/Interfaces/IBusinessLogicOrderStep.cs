namespace Mercury.Interfaces
{
    using System.Collections.Generic;

    public interface IBusinessLogicOrderStep<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        IEnumerable<TItem> Order(TContext context, IEnumerable<TItem> items);
    }
}
