namespace Mercury.Utils
{
    using System;
    using System.Collections.Generic;
    using Mercury.Interfaces;
    using static Mercury.Fundamentals.BusinessLogic;

    public class GenericBetterAlternativeFilter<TBusinessData, TSearchRequest, TItem> : IBusinessLogicFilterStatefulPredicate<TBusinessData, TSearchRequest, TItem>
    {
        public static GenericBetterAlternativeFilter<TBusinessData, TSearchRequest, TItem> FilterCheaperPrice<TIdentity, TPrice>(
            Func<TItem, TIdentity> identity, Func<TItem, TPrice> price)
            where TPrice : IComparable
        {
            ComparisonResult Compare(TBusinessData businessData, TSearchRequest searchRequest, TItem existingItem, TItem newItem)
            {
                if (!identity(existingItem).Equals(identity(newItem)))
                {
                    return ComparisonResult.NotComparable;
                }

                var newPrice = price(newItem);
                var existingPrice = price(existingItem);
                var newItemIsCheaper = newPrice.CompareTo(existingPrice) < 0;
                return newItemIsCheaper ?
                    ComparisonResult.BetterAlternative :
                    ComparisonResult.NotBetterAlternative;
            }

            return new GenericBetterAlternativeFilter<TBusinessData, TSearchRequest, TItem>(Compare);
        }

        public Func<TBusinessData, TSearchRequest, TItem, TItem, ComparisonResult> CompareTo { get; }

        public GenericBetterAlternativeFilter(Func<TBusinessData, TSearchRequest, TItem, TItem, ComparisonResult> compareTo)
        {
            this.CompareTo = compareTo;
        }

        private readonly List<TItem> previouslySeenItems = new List<TItem>();

        ReplaceableOption<TItem> IBusinessLogicFilterStatefulPredicate<TBusinessData, TSearchRequest, TItem>.BetterMatch(TBusinessData businessData, TSearchRequest searchRequest, TItem newItem)
        {
            for (var i = 0; i < this.previouslySeenItems.Count; i++)
            {
                TItem previouslySeenItem = this.previouslySeenItems[i];
                switch (this.CompareTo(businessData, searchRequest, previouslySeenItem, newItem))
                {
                    case ComparisonResult.NotComparable:
                        continue;

                    case ComparisonResult.NotBetterAlternative:
                        return ReplaceableOption<TItem>.None;

                    case ComparisonResult.BetterAlternative:
                        this.previouslySeenItems[i] = newItem;
                        return ReplaceableOption<TItem>.NewReplaceEntry(
                            oldItem: previouslySeenItem,
                            newItem: newItem);
                }
            }

            this.previouslySeenItems.Add(newItem);
            return ReplaceableOption<TItem>.NewSome(item: newItem);
        }
    }
}