namespace Mercury.Interfaces;

public interface IBusinessLogicProjection<TBusinessData, TSearchRequest, TItem> : IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>
{
    TItem Map(TBusinessData businessData, TSearchRequest searchRequest, TItem item);
}
