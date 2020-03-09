namespace DataTypesFSharp

type FashionType = string

type FashionQuery = 
    { Size: int
      FashionType: FashionType }

type FashionItem =
    { Size: int
      FashionType: FashionType
      Price: decimal 
      Description: string
      StockKeepingUnitID : string }

type BusinessData =
    { Markup: Map<FashionType, decimal>
      DefaultMarkup: decimal }

type ProcessingContext =
    { Query: FashionQuery 
      BusinessData: BusinessData }

type SearchRequest =
    { RequestID : string
      ResponseTopic : string
      Query : FashionQuery  }

type SearchResponsePayload =
    { RequestID : string
      Response : FashionItem list }

type SearchResponse =
    { RequestID : string
      ResponseBlob : string }
      
module FashionTypes =
    let TShirt : FashionType = "T-Shirt"
    let Pullover : FashionType = "Pullover"
    let Shoes : FashionType = "Shoes"
    let Hat : FashionType = "Hat"
    let Throusers : FashionType = "Throusers"

open Interfaces
    
type MarkupAdder() =
    interface IBusinessLogicFilterProjection<ProcessingContext, FashionItem> with
        member this.Map(ctx, item) =
            let markup =
                match ctx.BusinessData.Markup.TryFind(item.FashionType) with
                    | Some value -> value
                    | None -> ctx.BusinessData.DefaultMarkup

            let newPrice = item.Price + markup
            { item with Price = newPrice }

type SizeFilter() =
    interface IBusinessLogicFilterPredicate<ProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.Size = item.Size

type FashionTypeFilter() =
    interface IBusinessLogicFilterPredicate<ProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.FashionType = item.FashionType
        
open System.Runtime.CompilerServices
// Don't want to do this https://stackoverflow.com/questions/18151969/can-we-get-access-to-the-f-copy-and-update-feature-from-c
[<Extension>]
module FashionItemExtensions =   
    [<Extension>]
    let WithPrice(fashionItem : FashionItem, newPrice : decimal) = 
        // This is a simple convenience extension method so that C# devs can say fashionItem.WithPrice(newPrice: fashionItem.Price + 1_00m)
        { fashionItem with Price = newPrice }
