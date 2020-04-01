namespace DataTypesFSharp

open System.Collections.Generic
open System.Runtime.CompilerServices
open Fundamentals

[<Extension>]
module BusinessDataExtensions =
    [<Extension>]
    let Update (businessData: BusinessData) (version: UpdateOffset) (update: BusinessDataUpdate): BusinessData =
        match update with
        | MarkupUpdate(fashionType, markupPrice) ->
            { businessData with
                  Version = version
                  Markup = businessData.Markup.Add(fashionType, markupPrice) }
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