module Mercury.Fundamentals.BusinessData

open Mercury.Fundamentals.Types

type BusinessData<'domainSpecificBusinessData> =
    { Data: 'domainSpecificBusinessData
      Offset: Offset }

type SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate> =
    'domainSpecificBusinessData -> 'domainSpecificUpdate -> 'domainSpecificBusinessData

let updateBusinessData<'domainSpecificBusinessData, 'domainSpecificUpdate> (domainSpecificUpdateFunction: SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate>) (businessData: BusinessData<'domainSpecificBusinessData>) (domainSpecificUpdateMessage: Message<'domainSpecificUpdate>): BusinessData<'domainSpecificBusinessData> =
    { businessData with
        Offset = domainSpecificUpdateMessage.Offset
        Data = domainSpecificUpdateFunction businessData.Data domainSpecificUpdateMessage.Payload }
