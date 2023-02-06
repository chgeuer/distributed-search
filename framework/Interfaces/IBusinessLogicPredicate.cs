namespace Mercury.Interfaces;

public interface IBusinessLogicPredicate<TBusinessData, TSearchRequest, TItem> : IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>
{
    bool Matches(TBusinessData businessData, TSearchRequest searchRequest, TItem item);
}