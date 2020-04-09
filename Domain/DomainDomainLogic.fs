namespace Fashion

open Fashion.Domain
open Fashion.BusinessData

open Interfaces

type MarkupAdder() =
    interface IBusinessLogicFilterProjection<FashionProcessingContext, FashionItem> with
        member this.Map(ctx, item) =
            let markup =
                match ctx.BusinessData.Markup.TryFind(item.FashionType) with
                | Some value -> value
                | None        -> ctx.BusinessData.DefaultMarkup
            let newPrice = item.Price + markup

            { item with Price = newPrice }

type SizeFilter() =
    interface IBusinessLogicFilterPredicate<FashionProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.Size = item.Size

type FashionTypeFilter() =
    interface IBusinessLogicFilterPredicate<FashionProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.FashionType = item.FashionType

open System.Runtime.CompilerServices

// Don't want to do this https://stackoverflow.com/questions/18151969/can-we-get-access-to-the-f-copy-and-update-feature-from-c

[<Extension>]
module FashionExtensions =
    [<Extension>]
    let WithPrice(fashionItem: FashionItem, newPrice: decimal) =
        // This is a simple convenience extension method so that C# devs can say fashionItem.WithPrice(newPrice: fashionItem.Price + 1_00m)
        { fashionItem with Price = newPrice }

open BusinessLogic.Logic

module Steps =
    let ProperSize (ctx: FashionProcessingContext) (item: FashionItem) =
        ctx.Query.Size = item.Size

    let ProperFashionType (ctx: FashionProcessingContext) (item: FashionItem) =
        ctx.Query.FashionType = item.FashionType

    let StepCollection : PipelineSteps2<FashionProcessingContext, FashionItem> =
        { StreamingSteps = [];
          SequentialSteps = [] }
