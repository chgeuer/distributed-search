namespace Mercury.Interfaces;

using static Mercury.Fundamentals.BusinessLogic;

public interface IBusinessLogicFilterStatefulPredicate<TBusinessData, TSearchRequest, TItem> : IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>
{
    ReplaceableOption<TItem> BetterMatch(TBusinessData businessData, TSearchRequest searchRequest, TItem item);
}
