namespace Mercury.Interfaces
{
    public interface IBusinessLogicPredicate<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        bool Matches(TContext context, TItem item);
    }
}