namespace DataTypesFSharp

open System.Runtime.CompilerServices

[<Extension>]
module BusinessDataExtensions =   
    [<Extension>]
    let AddMarkup(businessData: BusinessData, version: UpdateOffset, update: BusinessDataUpdateMarkup) =
        { businessData with Version = version ; Markup = businessData.Markup.Add(update.FashionType, update.MarkupPrice) }
