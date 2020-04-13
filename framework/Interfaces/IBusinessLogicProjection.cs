namespace Mercury.Interfaces
{
    public interface IBusinessLogicProjection<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        TItem Map(TContext context, TItem item);
    }
}
