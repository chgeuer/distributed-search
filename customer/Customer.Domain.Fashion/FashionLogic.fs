namespace Mercury.Customer.Fashion

open System.Collections.Generic
open System.Runtime.CompilerServices
open Mercury.Customer.Fashion.Domain
open Mercury.Customer.Fashion.BusinessData
open Mercury.Interfaces

type MarkupAdder() =
    interface IBusinessLogicFilterProjection<FashionProcessingContext, FashionItem> with
        member this.Map(ctx, item) =
            let markup =
                match ctx.BusinessData.Markup.TryFind(item.FashionType) with
                | Some value -> value
                | None       -> ctx.BusinessData.DefaultMarkup
            let newPrice = item.Price + markup

            { item with Price = newPrice }

type SizeFilter() =
    interface IBusinessLogicFilterPredicate<FashionProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.Size = item.Size

type FashionTypeFilter() =
    interface IBusinessLogicFilterPredicate<FashionProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.FashionType = item.FashionType

// Don't want to do this https://stackoverflow.com/questions/18151969/can-we-get-access-to-the-f-copy-and-update-feature-from-c

//module Steps =
//    let ProperSize (ctx: FashionProcessingContext) (item: FashionItem) =
//        ctx.Query.Size = item.Size

//    let ProperFashionType (ctx: FashionProcessingContext) (item: FashionItem) =
//        ctx.Query.FashionType = item.FashionType

//    let StepCollection : PipelineSteps2<FashionProcessingContext, FashionItem> =
//        { StreamingSteps = [];
//          SequentialSteps = [] }

[<Extension>]
module FashionExtensions =
    [<Extension>]
    let WithPrice(fashionItem: FashionItem, newPrice: decimal) =
        // This is a simple convenience extension method so that C# devs can say fashionItem.WithPrice(newPrice: fashionItem.Price + 1_00m)
        { fashionItem with Price = newPrice }

    [<Extension>]
    let ApplyFashionUpdate businessData update =
        match update with
        | MarkupUpdate(fashionType, markupPrice) ->
            match markupPrice with
            | price when price <= 0m ->
                { businessData with
                      Markup = businessData.Markup.Remove(fashionType) }
            | price -> 
                { businessData with
                      Markup = businessData.Markup.Add(fashionType, price) }
        | BrandUpdate(key, value) ->
            { businessData with
                  Brands = businessData.Brands.Add(key, value) }
        | SetDefaultMarkup newDefaultPrice ->
            { businessData with
                  DefaultMarkup = newDefaultPrice }

    [<Extension>]
    let ApplyFashionUpdates (businessData: FashionBusinessData) (updates: IEnumerable<FashionBusinessDataUpdate>): FashionBusinessData =
        updates
        |> List.ofSeq
        |> List.fold ApplyFashionUpdate businessData
