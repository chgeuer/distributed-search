namespace Mercury.Utils
{
    using System;
    using System.Collections.Generic;
    using Mercury.Interfaces;
    using static BusinessLogic.Logic;

    public class GenericBetterAlternativeFilter<TContext, TItem> : IBusinessLogicFilterStatefulPredicate<TContext, TItem>
    {
        public static GenericBetterAlternativeFilter<TContext, TItem> FilterCheaperPrice<TIdentity, TPrice>(
            Func<TItem, TIdentity> identity, Func<TItem, TPrice> price)
            where TPrice : IComparable
        {
            ComparisonResult Compare(TContext ignoredContext, TItem existingItem, TItem newItem)
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

            return new GenericBetterAlternativeFilter<TContext, TItem>(Compare);
        }

        public Func<TContext, TItem, TItem, ComparisonResult> CompareTo { get; }

        public GenericBetterAlternativeFilter(Func<TContext, TItem, TItem, ComparisonResult> compareTo)
        {
            this.CompareTo = compareTo;
        }

        private readonly List<TItem> previouslySeenItems = new List<TItem>();

        ReplaceableOption<TItem> IBusinessLogicFilterStatefulPredicate<TContext, TItem>.BetterMatch(TContext context, TItem newItem)
        {
            for (var i = 0; i < this.previouslySeenItems.Count; i++)
            {
                TItem previouslySeenItem = this.previouslySeenItems[i];
                switch (this.CompareTo(context, previouslySeenItem, newItem))
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