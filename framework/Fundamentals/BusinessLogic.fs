module Mercury.BusinessLogic.Logic

type Predicate<'context, 'item> =
    'context -> 'item -> bool

type Projection<'context, 'item> =
    'context -> 'item -> 'item

type PipelineStep<'context, 'item> =
    | Predicate of Predicate<'context, 'item>
    | Projection of Projection<'context, 'item>

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
