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

module FashionTypes =
    let TShirt: FashionType = "T-Shirt"
    let Pullover: FashionType = "Pullover"
    let Shoes: FashionType = "Shoes"
    let Hat: FashionType = "Hat"
    let Throusers: FashionType = "Throusers"
