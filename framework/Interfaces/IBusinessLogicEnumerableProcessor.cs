namespace Mercury.Interfaces
{
    using System.Collections.Generic;

    // A business logic step which works on the full result collection
    public interface IBusinessLogicEnumerableProcessor<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        IEnumerable<TItem> Process(TContext context, IEnumerable<TItem> items);
    }
}
