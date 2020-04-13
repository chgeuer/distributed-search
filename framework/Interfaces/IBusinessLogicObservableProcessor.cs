namespace Mercury.Interfaces
{
    using System;

    public interface IBusinessLogicObservableProcessor<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        IObservable<TItem> Stream(TContext context, IObservable<TItem> items);
    }
}