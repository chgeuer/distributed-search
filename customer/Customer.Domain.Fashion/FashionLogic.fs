namespace Mercury.Customer.Fashion

open System.Runtime.CompilerServices
open Mercury.Customer.Fashion.Domain
open Mercury.Customer.Fashion.BusinessData
open Mercury.Interfaces

type MarkupAdder() =
    interface IBusinessLogicProjection<FashionBusinessData, FashionSearchRequest, FashionItem> with
        member this.Map(businessData, _, item) =
            let markup =
                match businessData.Markup.TryFind(item.FashionType) with
                | Some value -> value
                | None       -> businessData.DefaultMarkup
            let newPrice = item.Price + markup

            { item with Price = newPrice }

type SizeFilter() =
    interface IBusinessLogicPredicate<FashionBusinessData, FashionSearchRequest, FashionItem> with
        member this.Matches(_, searchRequest, item) = searchRequest.Size = item.Size

type FashionTypeFilter() =
    interface IBusinessLogicPredicate<FashionBusinessData, FashionSearchRequest, FashionItem> with
        member this.Matches(_, searchRequest, item) = searchRequest.FashionType = item.FashionType

type OrderByPriceFilter() =
    interface IBusinessLogicEnumerableProcessor<FashionBusinessData, FashionSearchRequest, FashionItem> with
        member this.Process(_, _, items) =
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
