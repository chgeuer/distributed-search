module Mercury.Customer.Fashion.Domain

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

let TShirt: FashionType = "T-Shirt"
let Pullover: FashionType = "Pullover"
let Shoes: FashionType = "Shoes"
let Hat: FashionType = "Hat"
let Throusers: FashionType = "Throusers"
