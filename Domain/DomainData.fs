namespace DataTypesFSharp

type FashionType = string

type FashionQuery =
    { Size: int
      FashionType: FashionType }

type FashionItem =
    { Size: int
      FashionType: FashionType
      Price: decimal
      Description: string
      StockKeepingUnitID: string }

type SearchRequest =
    { RequestID: string
      ResponseTopic: string
      Query: FashionQuery }

type SearchResponsePayload =
    { RequestID: string
      Response: FashionItem list }

open Interfaces

type SearchResponse =
    { RequestID: string
      ResponseBlob: string }
    interface IMessageEnrichableWithPayloadAddress with
        member this.GetPayloadAddress() = 
            this.ResponseBlob
        member this.SetPayloadAddress(address) = 
            { this with ResponseBlob = address } :> IMessageEnrichableWithPayloadAddress

module FashionTypes =
    let TShirt: FashionType = "T-Shirt"
    let Pullover: FashionType = "Pullover"
    let Shoes: FashionType = "Shoes"
    let Hat: FashionType = "Hat"
    let Throusers: FashionType = "Throusers"
