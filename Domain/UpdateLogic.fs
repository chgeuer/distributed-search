namespace DataTypesFSharp

open System.Runtime.CompilerServices

[<Extension>]
module BusinessDataExtensions =   
    [<Extension>]
    let AddMarkup(businessData: BusinessData, version: UpdateOffset, fashionType: FashionType, markupPrice: decimal) =
        { businessData with Version = version ; Markup = businessData.Markup.Add(fashionType, markupPrice) }
