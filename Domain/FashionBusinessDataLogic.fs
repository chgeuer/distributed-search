namespace Fashion

open System.Collections.Generic
open System.Runtime.CompilerServices
open Fashion.BusinessData

[<Extension>]
module FashionBusinessDataExtensions =
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
