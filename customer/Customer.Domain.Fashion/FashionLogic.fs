namespace Mercury.Customer.Fashion

open System.Runtime.CompilerServices
open Mercury.Customer.Fashion.Domain
open Mercury.Customer.Fashion.BusinessData
open Mercury.Interfaces

type MarkupAdder() =
    interface IBusinessLogicProjection<FashionProcessingContext, FashionItem> with
        member this.Map(ctx, item) =
            let markup =
                match ctx.BusinessData.Markup.TryFind(item.FashionType) with
                | Some value -> value
                | None       -> ctx.BusinessData.DefaultMarkup
            let newPrice = item.Price + markup

            { item with Price = newPrice }

type SizeFilter() =
    interface IBusinessLogicPredicate<FashionProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.Size = item.Size

type FashionTypeFilter() =
    interface IBusinessLogicPredicate<FashionProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.FashionType = item.FashionType

type OrderByPriceFilter() =
    interface IBusinessLogicEnumerableProcessor<FashionProcessingContext, FashionItem> with
        member this.Process(ctx, items) = 
            items
            |> List.ofSeq
            |> List.sortBy (fun (x: FashionItem) -> x.Price)
            |> List.toSeq

[<Extension>]
module FashionExtensions =
    [<Extension>]
    let WithPrice(fashionItem: FashionItem, newPrice: decimal) =
        // This is a simple convenience extension method so that C# devs can say fashionItem.WithPrice(newPrice: fashionItem.Price + 1_00m)
        { fashionItem with Price = newPrice }
