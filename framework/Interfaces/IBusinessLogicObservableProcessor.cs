namespace Mercury.Interfaces
{
    using System;

    public interface IBusinessLogicObservableProcessor<TBusinessData, TSearchRequest, TItem> : IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>
    {
        IObservable<TItem> Stream(TBusinessData businessData, TSearchRequest searchRequest, IObservable<TItem> items);
    }
}