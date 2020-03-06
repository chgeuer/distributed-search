namespace Interfaces
{
    using Fundamentals;
    using System;
    using System.Collections.Generic;

    public class GenericBetterAlternativeFilter<TContext, TItem> : IBusinessLogicFilterStatefulPredicate<TContext, TItem>
    {
        public static GenericBetterAlternativeFilter<TContext, TItem> FilterCheaperPrice<TIdentity, TPrice>(
            Func<TItem, TIdentity> identity, Func<TItem, TPrice> price) where TPrice : IComparable
        {
            ComparisonResult compare(TContext _, TItem existingItem, TItem newItem)
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

            return new GenericBetterAlternativeFilter<TContext, TItem>(compare);
        }

        public Func<TContext, TItem, TItem, ComparisonResult> CompareTo { get; }

        public GenericBetterAlternativeFilter(Func<TContext, TItem, TItem, ComparisonResult> compareTo) { this.CompareTo = compareTo; }

        private readonly List<TItem> PreviouslySeenItems = new List<TItem>();

        ReplaceableOption<TItem> IBusinessLogicFilterStatefulPredicate<TContext, TItem>.BetterMatch(TContext context, TItem newItem)
        {
            for (var i = 0; i < PreviouslySeenItems.Count; i++)
            {
                TItem previouslySeenItem = PreviouslySeenItems[i];
                switch (this.CompareTo(context, previouslySeenItem, newItem))
                {
                    case ComparisonResult.NotComparable:
                        continue;

                    case ComparisonResult.NotBetterAlternative:
                        return ReplaceableOption<TItem>.None;

                    case ComparisonResult.BetterAlternative:
                        PreviouslySeenItems[i] = newItem;
                        return ReplaceableOption<TItem>.NewReplaceEntry(
                            oldItem: previouslySeenItem,
                            newItem: newItem);
                }
            }

            PreviouslySeenItems.Add(newItem);
            return ReplaceableOption<TItem>.NewSome(item: newItem);
        }
    }
}