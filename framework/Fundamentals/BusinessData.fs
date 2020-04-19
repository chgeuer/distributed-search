module Mercury.Fundamentals.BusinessData

open System
open Mercury.Fundamentals.Types

type UpdateOutOfOrderError =
    { DataOffset: Offset 
      UpdateOffset: Offset }

type BusinessDataUpdateError = 
    | UpdateOutOfOrderError of UpdateOutOfOrderError
    | SnapshotDownloadError of Exception

type BusinessData<'domainSpecificBusinessData> =
    { Data: 'domainSpecificBusinessData
      Offset: Offset }

type SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate> =
    'domainSpecificBusinessData -> 'domainSpecificUpdate -> 'domainSpecificBusinessData

let updateBusinessData<'domainSpecificBusinessData, 'domainSpecificUpdate> (func: SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate>) (data: Result<BusinessData<'domainSpecificBusinessData>, BusinessDataUpdateError>) (um: Message<'domainSpecificUpdate>): Result<BusinessData<'domainSpecificBusinessData>, BusinessDataUpdateError> =
    match data with
    | Error e -> Error e
    | Ok data ->
        let newVersion = 
            match (data.Offset, um.Offset) with
            | (dataOffset, updateOffset) when dataOffset.Add(1L) <> updateOffset -> 
                Error { DataOffset = dataOffset; UpdateOffset = updateOffset }
            | (_, updateOffset) -> Ok updateOffset

        match newVersion with
            | Error e -> Error (UpdateOutOfOrderError e)
            | Ok newVersion -> 
                let newData = func data.Data um.Payload
                Ok { data with Offset = newVersion; Data = newData }
