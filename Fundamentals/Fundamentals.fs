namespace Fundamentals

type ComparisonResult =
  | NotComparable = 0
  | NotBetterAlternative = -1
  | BetterAlternative = 1

type ReplaceableOption<'a> =
  | None
  | Some of 'a
  | ReplaceEntry of OldItem:'a * NewItem:'a
