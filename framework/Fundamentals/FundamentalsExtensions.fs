﻿namespace Mercury.Fundamentals.Extensions

open System.Runtime.CompilerServices
open Mercury.Fundamentals.Types

type Configuration<'k, 'v when 'k: comparison> =
    { Watermark: Watermark
      Data: Map<'k, 'v> }

type UpdateOperation<'k, 'v> =
    | Add of Key: 'k * Value: 'v
    | Remove of Key: 'k

type Update =
    { Watermark: Watermark
      UpdateArea: string
      Operation: UpdateOperation<string, string> }

type UpdateableData =
    { Watermark: Watermark
      Data: Map<string, Map<string, string>> }

[<Extension>]
module UpdateExtensions =
    let ApplyUpdateOperation (data: Map<'k, 'v>) (update: UpdateOperation<'k, 'v>) : Map<'k, 'v> =
        match update with
        | Add(key, value) -> data.Add(key, value)
        | Remove key -> data.Remove(key)

    [<Extension>]
    let ApplyUpdateOperations (data: Map<'k, 'v>) (updates: UpdateOperation<'k, 'v> list): Map<'k, 'v> =
        updates |> List.fold ApplyUpdateOperation data

    [<Extension>]
    let ApplyUpdate (data: UpdateableData) (update: Update): UpdateableData =
        let map =
            if data.Data.ContainsKey(update.UpdateArea)
            then update.Operation |> ApplyUpdateOperation(data.Data.Item(update.UpdateArea))
            else Map.empty
        { data with
              Watermark = update.Watermark
              Data = data.Data.Add(update.UpdateArea, map) }

    [<Extension>]
    let ApplyUpdates (data: UpdateableData) (updates: Update list): UpdateableData =
        updates |> List.fold ApplyUpdate data
