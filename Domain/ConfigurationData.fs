namespace DataTypesFSharp

open Fundamentals

type BusinessData =
    { Markup: Map<FashionType, decimal>
      Brands: Map<string, string>
      DefaultMarkup: decimal
      Version: UpdateOffset }

type ProcessingContext =
    { Query: FashionQuery
      BusinessData: BusinessData }

type BusinessDataUpdate =
    | MarkupUpdate of FashionType: FashionType * MarkupPrice: decimal
    | BrandUpdate of BrandAcronym: string * Name: string
