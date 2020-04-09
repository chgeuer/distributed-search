namespace MyTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Microsoft.FSharp.Collections;
    using NUnit.Framework;
    using Interfaces;
    using Fundamentals.Extensions;
    using Fashion;
    using Fashion.Domain;
    using static Fundamentals.Types;
    using static BusinessLogic.Logic;
    using static Fashion.BusinessData;

    public class VersionedDictionary<T>
    {
        public long Offset { get; set; }
        public Dictionary<string, T> Values { get; set; }
        public T this[string i] { get => this.Values[i]; }
    }

    public class Tests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void TestGenericUpdates2() 
        {
            var data = new UpdateableData(
                offset: Offset.NewOffset(0),
                data: new FSharpMap<string, FSharpMap<string, string>>(
                    Array.Empty<Tuple<string, FSharpMap<string, string>>>()));
            Assert.AreEqual(0, data.Data.Count);

            static UpdateOperation<string, string> Add(string key, string value) => UpdateOperation<string, string>.NewAdd(key, value);
            static UpdateOperation<string, string> Remove(string key) => UpdateOperation<string, string>.NewRemove(key);

            var updates = new[] {
                //new Update(offset: Offset.NewOffset(15), updateArea: "currencyExchange", Add("USD-to-GBP", "1.2")),
                //new Update(offset: Offset.NewOffset(15), updateArea: "airports", Add("HLR", "{  'legalName' = 'London Heathrow'   }")),
                new Update(offset: Offset.NewOffset(20), updateArea: "brands", Add("DG", "Docker and Gabana")),
                new Update(offset: Offset.NewOffset(23), updateArea: "brands", Add("DÖ", "Diöhr")),
                new Update(offset: Offset.NewOffset(24), updateArea: "brands", Remove("DG")),
            }.ToFSharp();

            var updatedData = data.ApplyUpdates(updates);
            Assert.AreEqual(1, updatedData.Data.Count);

            Console.WriteLine(updatedData.AsJSON());
        }

        [Test]
        public void TestGenericUpdates()
        {
            static UpdateOperation<string, int> Add(string key, int value) => UpdateOperation<string, int>.NewAdd(key, value);
            static UpdateOperation<string, int> Remove(string key) => UpdateOperation<string, int>.NewRemove(key);
            
            var input = "{ \"hallo\": 2 }";

            var ops = new[] {
                Add("Foo", 3),
                Add("Foo", 4),
                Remove("hallo"),
                Add("Bar", 2),
                Remove("hallo"),
            }.ToFSharp();

            var expected = "{\"Bar\":2,\"Foo\":4}";

            var result =
                input
                .DeserializeJSON<FSharpMap<string, int>>()
                .ApplyUpdateOperations(ops);

            Assert.AreEqual(expected.DeserializeJSON<FSharpMap<string, int>>(), result);
        }

        [Test]
        public void TestGenericUpdates3()
        {
            static UpdateOperation<string, string> Add(string key, string value) => UpdateOperation<string, string>.NewAdd(key, value);
            static UpdateOperation<string, string> Remove(string key) => UpdateOperation<string, string>.NewRemove(key);

            var updates = new[]{
                new Update(offset: Offset.NewOffset(2), "f", Add("f3", "f4")),
                new Update(offset: Offset.NewOffset(3), "f", Remove("f1"))
            }.ToFSharp();

            var data = new UpdateableData(
                offset: Offset.NewOffset(1),
                data: new[] {
                    ("f", new[] { ("f1", "f2") }),
                    ("a", new[] { ("a1", "a2"), ("a3", "a4") }),
                }.ToFSharpMapOfMaps());

            var postUpdate = data.ApplyUpdates(updates);

            Assert.IsFalse(postUpdate.Data["f"].ContainsKey("13"));
            Assert.IsTrue(postUpdate.Data["f"].ContainsKey("f3"));

            Console.WriteLine(postUpdate.AsJSON());
        }

        [Test]
        public void TestDomainSpecificUpdates()
        {
            var updates = new FashionBusinessDataUpdate[]
                {
                    FashionBusinessDataUpdate.NewMarkupUpdate(FashionTypes.Throusers, 2_00m),
                    FashionBusinessDataUpdate.NewBrandUpdate("BB", "Bruno Banano")
                }
                .ToFSharp();
            
            var v1 = new FashionBusinessData(
                      markup: new FSharpMap<string, decimal>(new[]
                      {
                            Tuple.Create(FashionTypes.Hat, 0_12m),
                            Tuple.Create(FashionTypes.Throusers, 1_50m),
                      }),
                      brands: new FSharpMap<string, string>(new[]
                      {
                          Tuple.Create("DG", "Docker and Galbani")
                      }),
                      defaultMarkup: 1_00);

            var v2 = v1.ApplyFashionUpdates(updates.Take(1)); // only apply a single update
            var v3 = v1.ApplyFashionUpdates(updates); // apply all updates
            var v3_2 = v2.ApplyFashionUpdates(updates.Skip(1)); // apply only last update

            Assert.AreEqual(v1.Markup[FashionTypes.Hat], 0_12m);
            Assert.AreEqual(v1.Markup[FashionTypes.Throusers], 1_50m);
            Assert.IsFalse(v1.Brands.ContainsKey("BB"));

            Assert.AreEqual(v2.Markup[FashionTypes.Hat], 0_12m);
            Assert.AreEqual(v2.Markup[FashionTypes.Throusers], 2_00m);
            Assert.IsFalse(v2.Brands.ContainsKey("BB"));

            Assert.AreEqual(v3.Markup[FashionTypes.Hat], 0_12m);
            Assert.AreEqual(v3.Markup[FashionTypes.Throusers], 2_00m);
            Assert.IsTrue(v3.Brands.ContainsKey("DG"));
            Assert.IsTrue(v3.Brands.ContainsKey("BB"));

            Assert.AreEqual(v3, v3_2, "v3 == v3_2");
        }

        [Test]
        public void Test1()
        {
            static Func<FashionProcessingContext, FashionItem, bool> NewStatefulFilter()
            {
                var bySizeDict = new Dictionary<int, FashionItem>();
                bool emitNewEntry(FashionProcessingContext _, FashionItem i)
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

            IBusinessLogicFilterStatefulPredicate<FashionProcessingContext, FashionItem> createF()
            {
                static ComparisonResult comparePrice(FashionProcessingContext _, FashionItem existingItem, FashionItem newItem)
                {
                    if (existingItem.StockKeepingUnitID != newItem.StockKeepingUnitID)
                    {
                        return ComparisonResult.NotComparable;
                    }

                    return newItem.Price < existingItem.Price
                        ? ComparisonResult.BetterAlternative
                        : ComparisonResult.NotBetterAlternative;
                }

                return new GenericBetterAlternativeFilter<FashionProcessingContext, FashionItem>(comparePrice);
            }

            IEnumerable<IBusinessLogicStep<FashionProcessingContext, FashionItem>> CreatePipeline() =>
                new IBusinessLogicStep<FashionProcessingContext, FashionItem>[]
                {
                    new GenericFilter<FashionProcessingContext, FashionItem>((c,i) => c.Query.Size == i.Size),
                    new GenericFilter<FashionProcessingContext, FashionItem>(NewStatefulFilter()),
                    new MarkupAdder(),
                };

            var fashionBusinessData = new FashionBusinessData(
                markup: new FSharpMap<string, decimal>(new[] {
                    Tuple.Create(FashionTypes.Hat, 0_12m),
                    Tuple.Create(FashionTypes.Throusers, 1_50m),
                }),
                brands: new FSharpMap<string, string>(new[]
                {
                    Tuple.Create("DG", "Docker and Galbani")
                }),
                defaultMarkup: 1_00);
            
            var businessData = new BusinessData<FashionBusinessData>(
                fashionBusinessData,
                Offset.NewOffset(1));

            var query = new FashionSearchRequest(size: 16, fashionType: FashionTypes.Hat);
            var ctx2 = new FashionProcessingContext(query: query, businessData: fashionBusinessData);

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