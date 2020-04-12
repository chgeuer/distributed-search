namespace Mercury.Interfaces
{
    using static Mercury.Fundamentals.BusinessLogic;

    public interface IBusinessLogicFilterStatefulPredicate<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        ReplaceableOption<TItem> BetterMatch(TContext context, TItem item);
    }
}
