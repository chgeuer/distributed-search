module Mercury.Fundamentals.Types

open System
open System.IO
open System.Threading
open System.Threading.Tasks

type StorageOffloadFunctions =
    { Upload: Func<string, Stream, CancellationToken, Task> 
      Download: Func<string, CancellationToken, Task<Stream>> }

type Offset = Offset of int64

type SeekPosition =
    | FromOffset of Offset: Offset 
    | FromTail

type RequestID = string

type Message<'payload> =
    { RequestID: RequestID option
      Offset: Offset
      Payload: 'payload }

type TopicPartitionID =
    { TopicName: string
      ComputeNodeId: int option }

type ProviderSearchRequest<'searchRequest> =
    { RequestID: RequestID
      ResponseTopic: TopicPartitionID
      SearchRequest: 'searchRequest }

type ProviderSearchResponse<'item> =
    { RequestID: RequestID
      Response: 'item list }

type BlobStorageAddress = BlobStorageAddress of string

module BlobStorageAddress =
    let create address =
        if String.IsNullOrEmpty address then
            failwith "address must not be null or empty"
        else 
            BlobStorageAddress address
    let value (BlobStorageAddress address) =
        address

type StorageOffloadReference =
    { Address: BlobStorageAddress }

type BusinessData<'domainSpecificBusinessData> =
    { Data: 'domainSpecificBusinessData
      Offset: Offset }

type SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate> =
    'domainSpecificBusinessData -> 'domainSpecificUpdate -> 'domainSpecificBusinessData

let updateBusinessData<'domainSpecificBusinessData, 'domainSpecificUpdate> (domainSpecificUpdateFunction: SingleUpdate<'domainSpecificBusinessData, 'domainSpecificUpdate>) (businessData: BusinessData<'domainSpecificBusinessData>) (domainSpecificUpdateMessage: Message<'domainSpecificUpdate>): BusinessData<'domainSpecificBusinessData> =
    { businessData with
        Offset = domainSpecificUpdateMessage.Offset
        Data = domainSpecificUpdateFunction businessData.Data domainSpecificUpdateMessage.Payload }

