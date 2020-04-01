namespace Fundamentals

open System.Runtime.CompilerServices

type ComparisonResult =
    | NotComparable = 0
    | NotBetterAlternative = -1
    | BetterAlternative = 1

type ReplaceableOption<'a> =
    | None
    | Some of 'a
    | ReplaceEntry of OldItem: 'a * NewItem: 'a

type UpdateOffset = int64

type Configuration<'k, 'v when 'k: comparison> =
    { Offset: UpdateOffset
      Data: Map<'k, 'v> }

type UpdateOperation<'k, 'v> =
    | Add of Key: 'k * Value: 'v
    | Remove of Key: 'k

[<Extension>]
module UpdateExtensions =
    let ApplyUpdate (data: Map<'k, 'v>) (update: UpdateOperation<'k, 'v>) : Map<'k, 'v> =
        match update with
        | Add(key, value) -> data.Add(key, value)
        | Remove key -> data.Remove(key)

    [<Extension>]
    let ApplyUpdates (data: Map<'k, 'v>) (updates: UpdateOperation<'k, 'v> list): Map<'k, 'v> =
        updates |> List.fold ApplyUpdate data

type Update =
    { Offset: UpdateOffset
      UpdateArea: string
      Operation: UpdateOperation<string, string> }

type UpdateableData =
    { Offset: UpdateOffset
      Data: Map<string, Map<string, string>> }

[<Extension>]
module UpdateDataExtensions =
    [<Extension>]
    [<CompiledName("Update")>]
    let ApplyUpdate (data: UpdateableData) (update: Update): UpdateableData =
        let map =
            if data.Data.ContainsKey(update.UpdateArea)
            then update.Operation |> UpdateExtensions.ApplyUpdate(data.Data.Item(update.UpdateArea))
            else Map.empty
        { data with
              Offset = update.Offset
              Data = data.Data.Add(update.UpdateArea, map) }

    [<Extension>]
    let ApplyUpdates (data: UpdateableData) (updates: Update list): UpdateableData =
        updates |> List.fold ApplyUpdate data
