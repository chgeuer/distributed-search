namespace Interfaces
{
    public interface IBusinessLogicFilterPredicate<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        bool Matches(TContext context, TItem item);
    }
}