module Fundamentals.Types

type UpdateOffset = int64

type RequestID = string

type Message<'payload> =
    { Payload: 'payload
      Offset: UpdateOffset
      Properties: Map<string, obj> }

type ResponseTopicAddress = string

type ProviderSearchRequest<'searchRequest> =
    { RequestID: RequestID
      ResponseTopic: ResponseTopicAddress
      SearchRequest: 'searchRequest }

type ProviderSearchResponse<'item> =
    { RequestID: RequestID
      Response: 'item list }

type BlobStorageAddress = string

type StorageOffloadReference =
    { RequestID: RequestID
      Address: BlobStorageAddress }

type SeekPosition =
    | FromOffset of UpdateOffset : UpdateOffset 
    | FromTail

//public class SeekPosition
//{
//    public static SeekPosition FromPosition(long position) => new SeekPosition { FromTail = false, Offset = position };
//    public static readonly SeekPosition Tail = new SeekPosition { FromTail = true, Offset = 0 };
//    public bool FromTail { get; private set; }
//    public long Offset { get; private set; }
//    private SeekPosition() { }
//}

//type IBusinessLogicStep<'context, 'item> = unit

//type PipelineSteps<'context, 'item> =
//    { StreamingSteps: IBusinessLogicStep<'context, 'item>
//      SequentialSteps: IBusinessLogicStep<'context, 'item> }

type ComparisonResult =
    | NotComparable = 0
    | NotBetterAlternative = -1
    | BetterAlternative = 1

type ReplaceableOption<'a> =
    | None
    | Some of 'a
    | ReplaceEntry of OldItem: 'a * NewItem: 'a

type Configuration<'k, 'v when 'k: comparison> =
    { Offset: UpdateOffset
      Data: Map<'k, 'v> }

type UpdateOperation<'k, 'v> =
    | Add of Key: 'k * Value: 'v
    | Remove of Key: 'k

type Update =
    { Offset: UpdateOffset
      UpdateArea: string
      Operation: UpdateOperation<string, string> }

type UpdateableData =
    { Offset: UpdateOffset
      Data: Map<string, Map<string, string>> }
