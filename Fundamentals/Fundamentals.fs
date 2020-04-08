module Fundamentals.Types

type Offset = Offset of int64

type SeekPosition =
    | FromOffset of Offset: Offset 
    | FromTail

//public class SeekPosition
//{
//    public static SeekPosition FromPosition(long position) => new SeekPosition { FromTail = false, Offset = position };
//    public static readonly SeekPosition Tail = new SeekPosition { FromTail = true, Offset = 0 };
//    public bool FromTail { get; private set; }
//    public long Offset { get; private set; }
//    private SeekPosition() { }
//}

type RequestID = string

type Message<'payload> =
    { RequestID: RequestID option
      Offset: Offset
      Payload: 'payload }

type TopicPartitionID =
    { TopicName: string
      PartitionId: int option }

type ProviderSearchRequest<'searchRequest> =
    { RequestID: RequestID
      ResponseTopic: TopicPartitionID
      SearchRequest: 'searchRequest }

type ProviderSearchResponse<'item> =
    { RequestID: RequestID
      Response: 'item list }

type BlobStorageAddress = string

type StorageOffloadReference =
    { Address: BlobStorageAddress }

open System
open System.IO
open System.Threading
open System.Threading.Tasks

type StorageOffloadFunctions =
    { Upload: Func<string, Stream, CancellationToken, Task> 
      Download: Func<string, CancellationToken, Task<Stream>> }
