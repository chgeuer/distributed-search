namespace DataTypesFSharp

open System.Collections.Generic
open System.Runtime.CompilerServices
open Fundamentals.Types

[<Extension>]
module BusinessDataExtensions =
    [<Extension>]
    let Update (businessData: BusinessData) (version: UpdateOffset) (update: BusinessDataUpdate): BusinessData =
        match update with
        | MarkupUpdate(fashionType, markupPrice) ->
            match markupPrice with
            | price when price <= 0m ->
                { businessData with
                      Version = version
                      Markup = businessData.Markup.Remove(fashionType) }
            | price -> 
                { businessData with
                      Version = version
                      Markup = businessData.Markup.Add(fashionType, price) }
        | BrandUpdate(key, value) ->
            { businessData with
                  Version = version
                  Brands = businessData.Brands.Add(key, value) }
        | SetDefaultMarkup newDefaultPrice ->
            { businessData with
                  Version = version
                  DefaultMarkup = newDefaultPrice }

    [<Extension>]
    let ApplyUpdates (businessData: BusinessData) (updates: IEnumerable<UpdateOffset * BusinessDataUpdate>): BusinessData =
        updates
        |> List.ofSeq
        |> List.fold (fun d (o, u) -> Update d o u) businessData
