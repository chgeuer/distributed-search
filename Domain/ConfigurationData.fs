namespace DataTypesFSharp

open Fundamentals

type BusinessData =
    { Markup: Map<FashionType, decimal>
      Brands: Map<string, string>
      DefaultMarkup: decimal
      Version: UpdateOffset }

//type MarkupBusinessData =
//    { Markup: Map<FashionType, decimal>
//      DefaultMarkup: decimal
//      Version: UpdateOffset }

//type BrandCompany = CompanyName of string
      
//type BrandsBusinessData =
//    { Brands: Map<string, BrandCompany >
//      Version: UpdateOffset }
      
type ProcessingContext =
    { Query: FashionSearchRequest
      BusinessData: BusinessData }

type BusinessDataUpdate =
    | MarkupUpdate of FashionType: FashionType * MarkupPrice: decimal
    | BrandUpdate of BrandAcronym: string * Name: string
    | SetDefaultMarkup of DefaultMarkupPrice: decimal
    // | AirportNameUpdate of AirportAcronym: string * LegalName: string
    // | AirportLocationUpdates of AirportAcronym: string  * Latitude: decimal * Longtitude: decimal
