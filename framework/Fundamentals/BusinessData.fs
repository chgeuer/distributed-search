namespace Mercury.Fundamentals

open System.Runtime.CompilerServices

[<Extension>]
module BusinessData =

    open System
    open Mercury.Fundamentals.Types

    type UpdateOutOfOrderError =
        { DataWatermark: Watermark
          UpdateWatermark: Watermark }

    type BusinessDataUpdateError =
        | UpdateOutOfOrderError of UpdateOutOfOrderError
        | SnapshotDownloadError of Exception

    type BusinessData<'domainSpecificBusinessData> =
        { Data: 'domainSpecificBusinessData
          Watermark: Watermark }

    type SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate> =
        'domainSpecificBusinessData -> 'domainSpecificUpdate -> 'domainSpecificBusinessData

    let updateBusinessData<'domainSpecificBusinessData, 'domainSpecificUpdate> (func: SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate>) (data: Result<BusinessData<'domainSpecificBusinessData>, BusinessDataUpdateError>) (um: WatermarkMessage<'domainSpecificUpdate>): Result<BusinessData<'domainSpecificBusinessData>, BusinessDataUpdateError> =
        match data with
        | Error e -> Error e
        | Ok data ->
            let newVersion =
                match (data.Watermark, um.Watermark) with
                | (dataWatermark, updateWatermark) when dataWatermark.Add(1L) <> updateWatermark ->
                    Error { DataWatermark = dataWatermark; UpdateWatermark= updateWatermark }
                | (_, updateWatermark) -> Ok updateWatermark

            match newVersion with
                | Error e -> Error (UpdateOutOfOrderError e)
                | Ok newVersion ->
                    let newData = func data.Data um.Payload
                    Ok { data with Watermark = newVersion; Data = newData }

    //[<Extension>]
    //let ApplyUpdates (data: UpdateableData) (updates: Update list): UpdateableData =
    //    updates |> List.fold ApplyUpdate data
