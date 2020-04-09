namespace Fashion.BusinessData

open Fashion.Domain

type FashionBusinessData =
    { Markup: Map<FashionType, decimal>
      Brands: Map<string, string>
      DefaultMarkup: decimal }

type FashionProcessingContext =
    { Query: FashionSearchRequest
      BusinessData: FashionBusinessData }

type FashionBusinessDataUpdate =
    | MarkupUpdate of FashionType: FashionType * MarkupPrice: decimal
    | BrandUpdate of BrandAcronym: string * Name: string
    | SetDefaultMarkup of DefaultMarkupPrice: decimal

module Code =
    let newFashionBusinessData(): FashionBusinessData =
        { Markup = Map.empty
          Brands = Map.empty
          DefaultMarkup = 0m }
