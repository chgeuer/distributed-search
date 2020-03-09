namespace MyTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using DataTypesFSharp;
    using Fundamentals;
    using Interfaces;
    using Microsoft.FSharp.Collections;
    using NUnit.Framework;

    public class Tests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void TestBusinessDataUpdate()
        {
            var v1 = new BusinessData(
              version: 1,
              markup: new FSharpMap<string, decimal>(new[]
              {
                    Tuple.Create(FashionTypes.Hat, 0_12m),
                    Tuple.Create(FashionTypes.Throusers, 1_50m),
              }),
              defaultMarkup: 1_00);

            var v2 = v1.AddMarkup(
                version: 2,
                FashionTypes.Throusers, 2_00m);

            Assert.AreEqual(v1.Version, 1);
            Assert.AreEqual(v1.Markup[FashionTypes.Hat], 0_12m);
            Assert.AreEqual(v1.Markup[FashionTypes.Throusers], 1_50m);
            Assert.AreEqual(v2.Version, 2);
            Assert.AreEqual(v2.Markup[FashionTypes.Hat], 0_12m);
            Assert.AreEqual(v2.Markup[FashionTypes.Throusers], 2_00m);
        }

        [Test]
        public void Test1()
        {
            static Func<ProcessingContext, FashionItem, bool> NewStatefulFilter()
            {
                var bySizeDict = new Dictionary<int, FashionItem>();
                bool emitNewEntry(ProcessingContext _, FashionItem i)
                {
                    var alreadyEmitted = bySizeDict.ContainsKey(i.Size);
                    if (!alreadyEmitted)
                    {
                        bySizeDict.Add(i.Size, i);
                    }
                    return !alreadyEmitted;
                }
                return emitNewEntry;
            }

            IBusinessLogicFilterStatefulPredicate<ProcessingContext, FashionItem> createF()
            {
                static ComparisonResult comparePrice(ProcessingContext _, FashionItem existingItem, FashionItem newItem)
                {
                    if (existingItem.StockKeepingUnitID != newItem.StockKeepingUnitID)
                    {
                        return ComparisonResult.NotComparable;
                    }

                    return newItem.Price < existingItem.Price
                        ? ComparisonResult.BetterAlternative
                        : ComparisonResult.NotBetterAlternative;
                }

                return new GenericBetterAlternativeFilter<ProcessingContext, FashionItem>(comparePrice);
            }

            IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>> CreatePipeline() =>
                new IBusinessLogicStep<ProcessingContext, FashionItem>[]
                {
                    new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.Size == i.Size),
                    new GenericFilter<ProcessingContext, FashionItem>(NewStatefulFilter()),
                    new MarkupAdder(),
                };

            var businessData = new BusinessData(
                markup: new FSharpMap<string, decimal>(new[] { 
                    Tuple.Create(FashionTypes.Hat, 0_12m),
                    Tuple.Create(FashionTypes.Throusers, 1_50m),
                }),
                defaultMarkup: 1_00,
                version: 0);

            var query = new FashionQuery(size: 16, fashionType: FashionTypes.Hat);
            var ctx2 = new ProcessingContext(query: query, businessData: businessData);

            var sufficientlyGoodHat = new FashionItem(size: 16, fashionType: FashionTypes.Hat, price: 12_00, description: "A nice large hat", stockKeepingUnitID: Guid.NewGuid().ToString());
            var sufficientlyGoodHatButTooExpensive = new FashionItem(size: 16, fashionType: FashionTypes.Hat, price: 12_50, description: "A nice large hat", stockKeepingUnitID: sufficientlyGoodHat.StockKeepingUnitID);
            var someThrouser = new FashionItem(size: 54, fashionType: FashionTypes.Throusers, price: 120_00, description: "A blue Jeans", stockKeepingUnitID: Guid.NewGuid().ToString());
            var aHatButTooSmall = new FashionItem(size: 15, fashionType: FashionTypes.Hat, price: 13_00, description: "A smaller hat", stockKeepingUnitID: Guid.NewGuid().ToString());
            var someDifferentHat = new FashionItem(size: 16, fashionType: FashionTypes.Hat, price: 12_00, description: "A different large hat", stockKeepingUnitID: Guid.NewGuid().ToString());

            var funcItems = new[] { sufficientlyGoodHat, someThrouser, aHatButTooSmall, someDifferentHat, sufficientlyGoodHatButTooExpensive };

            var list = funcItems
                .ApplySteps(ctx2, CreatePipeline())
                .ToList();
            Assert.AreEqual(list.Count, 1, "list.Count");
            Assert.AreEqual(list[0].StockKeepingUnitID, sufficientlyGoodHat.StockKeepingUnitID, "list[0].StockKeepingUnitID, sufficientlyGoodHat.StockKeepingUnitID");
            Assert.Greater(list[0].Price, funcItems[0].Price, "list[0].Price, funcItems[0].Price");

            Console.WriteLine($"Result {list[0].Description}");

            var list2 = funcItems
                .ToObservable()
                .ApplySteps(ctx2, CreatePipeline())
                .ToEnumerable().ToList();

            Assert.AreEqual(list2.Count, 1, "list.Count");
            Assert.AreEqual(list2[0].Description, funcItems[0].Description, "list[0].Description, funcItems[0].Description");
            Assert.Greater(list2[0].Price, funcItems[0].Price, "list[0].Price, funcItems[0].Price");

            Console.WriteLine($"Result {list2[0].Description}");
        }
    }
}