namespace Fundamentals

type ComparisonResult =
  | NotComparable = 0
  | NotBetterAlternative = -1
  | BetterAlternative = 1

type ReplaceableOption<'a> =
  | None
  | Some of 'a
  | ReplaceEntry of OldItem:'a * NewItem:'a

open System.Runtime.CompilerServices

// IEnumerable<UpdateOffset * BusinessDataUpdate> 

type Update<'k, 'v> =
  | Add of ('k * 'v)
  | Remove of 'k

[<Extension>]
module UpdateExtensions =   
    let Update (map: Map<'k,'v>) (update: Update<'k, 'v>) : Map<'k,'v> =
        match update with
        | Add(key, value) -> map.Add(key, value)
        | Remove (key) -> map.Remove(key)

    [<Extension>]
    let ApplyUpdatesXX (businessData: Map<'k,'v>) (updates: Update<'k, 'v> list) : Map<'k,'v> =
        let doUpdate = fun map update -> update |> Update map
        updates |> List.fold doUpdate businessData
