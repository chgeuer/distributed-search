namespace Mercury.Interfaces
{
    using static BusinessLogic.Logic;

    public interface IBusinessLogicFilterStatefulPredicate<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        ReplaceableOption<TItem> BetterMatch(TContext context, TItem item);
    }
}
