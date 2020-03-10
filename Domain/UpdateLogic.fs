namespace DataTypesFSharp

open System.Runtime.CompilerServices

[<Extension>]
module BusinessDataExtensions =   
    [<Extension>]
    let Update(businessData: BusinessData, version: UpdateOffset, update: BusinessDataUpdate) =
        match update with
        | MarkupUpdate(fashionType, markupPrice) -> { businessData with Version = version ; Markup = businessData.Markup.Add(fashionType, markupPrice) }
        | BrandUpdate(key, value) -> { businessData with Version = version ; Brands = businessData.Brands.Add(key, value) }
