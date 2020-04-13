namespace Mercury.Interfaces
{
    using System.Collections.Generic;

    // A business logic step which works on the full result collection
    public interface IBusinessLogicEnumerableProcessor<TBusinessData, TSearchRequest, TItem> : IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>
    {
        IEnumerable<TItem> Process(TBusinessData businessData, TSearchRequest searchRequest, IEnumerable<TItem> items);
    }
}
