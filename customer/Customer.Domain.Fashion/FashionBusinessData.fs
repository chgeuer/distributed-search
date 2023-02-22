module Mercury.Customer.Fashion.BusinessData

open System.Collections.Generic
open System.Runtime.CompilerServices
open Mercury.Customer.Fashion.Domain
open Mercury.Fundamentals.BusinessData

type FashionBusinessData =
    { Markup: Map<FashionType, decimal>
      Brands: Map<string, string>
      DefaultMarkup: decimal }

let newFashionBusinessData(): FashionBusinessData =
    { Markup = Map.empty
      Brands = Map.empty
      DefaultMarkup = 0m }

type FashionBusinessDataUpdate =
    | MarkupUpdate of FashionType: FashionType * MarkupPrice: decimal
    | BrandUpdate of BrandAcronym: string * Name: string
    | SetDefaultMarkup of DefaultMarkupPrice: decimal

[<Extension>]
module FashionBusinessDataExtensions =
    let update (businessData: FashionBusinessData) (update: FashionBusinessDataUpdate) : FashionBusinessData =
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
    let ApplyFashionUpdate : SingleUpdate<FashionBusinessData, FashionBusinessDataUpdate> =
        fun d u -> update d u

    [<Extension>]
    let ApplyFashionUpdates (businessData: FashionBusinessData) (updates: IEnumerable<FashionBusinessDataUpdate>): FashionBusinessData =
        updates
        |> List.ofSeq
        |> List.fold ApplyFashionUpdate businessData
