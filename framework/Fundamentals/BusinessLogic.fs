module Mercury.Fundamentals.BusinessLogic

open Mercury.Fundamentals

type Predicate<'context, 'item> =
    'context -> 'item -> bool

type Projection<'context, 'item> =
    'context -> 'item -> 'item

type IPredicate<'context, 'item> =
    abstract member Matches : Predicate<'context, 'item>

type IProjection<'context, 'item> =
    abstract member Map: Projection<'context, 'item>

type PipelineStep<'context, 'item> =
    | Predicate of Predicate<'context, 'item>
    | Projection of Projection<'context, 'item>

module PipelineStep =
    let createPredicate<'context,'item> (f: System.Func<'context,'item,bool>) : PipelineStep<'context, 'item> =
        f |> FSharpFuncUtil.ToFSharpFunc |> Predicate

    let createProjection<'context,'item> (f: System.Func<'context,'item,'item>) : PipelineStep<'context, 'item> =
        f |> FSharpFuncUtil.ToFSharpFunc |> Projection

type PipelineSteps2<'context, 'item> =
    { StreamingSteps:  PipelineStep<'context, 'item> list
      SequentialSteps: PipelineStep<'context, 'item> list }

type ComparisonResult =
    | NotComparable = 0
    | NotBetterAlternative = -1
    | BetterAlternative = 1

type ReplaceableOption<'a> =
    | None
    | Some of 'a
    | ReplaceEntry of OldItem: 'a * NewItem: 'a
