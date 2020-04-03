namespace DataTypesFSharp

type FashionType = string

type FashionSearchRequest =
    { Size: int
      FashionType: FashionType }

type FashionItem =
    { Size: int
      FashionType: FashionType
      Price: decimal
      Description: string
      StockKeepingUnitID: string }

open Interfaces

module FashionTypes =
    let TShirt: FashionType = "T-Shirt"
    let Pullover: FashionType = "Pullover"
    let Shoes: FashionType = "Shoes"
    let Hat: FashionType = "Hat"
    let Throusers: FashionType = "Throusers"

type RequestID = string

type ResponseTopicAddress = string

type ProviderSearchRequest<'searchRequest> =
    { RequestID: RequestID
      ResponseTopic: ResponseTopicAddress
      Query: 'searchRequest }

type ProviderSearchResponse<'a> =
    { RequestID: RequestID
      Response: 'a list }
