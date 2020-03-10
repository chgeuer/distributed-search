namespace DataTypesFSharp

type UpdateOffset = int64

type BusinessData =
    { Markup: Map<FashionType, decimal>
      DefaultMarkup: decimal
      Version: UpdateOffset }

type ProcessingContext =
    { Query: FashionQuery 
      BusinessData: BusinessData }
