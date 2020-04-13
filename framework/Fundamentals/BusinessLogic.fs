module Mercury.Fundamentals.BusinessLogic

open Mercury.Fundamentals

type Predicate<'businessData, 'searchRequest, 'item> =
    'businessData -> 'searchRequest -> 'item -> bool

type Projection<'businessData, 'searchRequest, 'item> =
    'businessData -> 'searchRequest -> 'item -> 'item

type IPredicate<'businessData, 'searchRequest, 'item> =
    abstract member Matches : Predicate<'businessData, 'searchRequest, 'item>

type IProjection<'businessData, 'searchRequest, 'item> =
    abstract member Map: Projection<'businessData, 'searchRequest, 'item>

type PipelineStep<'businessData, 'searchRequest, 'item> =
    | Predicate of Predicate<'businessData, 'searchRequest, 'item>
    | Projection of Projection<'businessData, 'searchRequest, 'item>

module PipelineStep =
    let createPredicate<'businessData, 'searchRequest,'item> (f: System.Func<'businessData, 'searchRequest,'item,bool>) : PipelineStep<'businessData, 'searchRequest, 'item> =
        f |> FSharpFuncUtil.ToFSharpFunc |> Predicate

    let createProjection<'businessData, 'searchRequest,'item> (f: System.Func<'businessData, 'searchRequest,'item,'item>) : PipelineStep<'businessData, 'searchRequest, 'item> =
        f |> FSharpFuncUtil.ToFSharpFunc |> Projection

type PipelineSteps2<'businessData, 'searchRequest, 'item> =
    { StreamingSteps:  PipelineStep<'businessData, 'searchRequest, 'item> list
      SequentialSteps: PipelineStep<'businessData, 'searchRequest, 'item> list }

type ComparisonResult =
    | NotComparable = 0
    | NotBetterAlternative = -1
    | BetterAlternative = 1

type ReplaceableOption<'a> =
    | None
    | Some of 'a
    | ReplaceEntry of OldItem: 'a * NewItem: 'a
