namespace Interfaces
{
    public interface IBusinessLogicFilterProjection<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        TItem Map(TContext context, TItem item);
    }
}
