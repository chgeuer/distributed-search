module Mercury.Fundamentals.Types

open System
open System.IO
open System.Threading
open System.Threading.Tasks

type StorageOffloadFunctions =
    { Upload: Func<string, Stream, CancellationToken, Task> 
      Download: Func<string, CancellationToken, Task<Stream>> }

type Watermark = Watermark of int64

type Watermark with
    member this.Add increment =
        let (Watermark watermarkValue) = this
        Watermark(watermarkValue + increment)

type SeekPosition =
    | FromWatermark of Watermark: Watermark 
    | FromTail

type RequestID = string

type Message<'payload> =
    { RequestID: RequestID option
      Watermark: Watermark
      Payload: 'payload }

type TopicName = string

type PartitionSpecification =
    | ComputeNodeID of int
    | PartitionID of int
    | RoundRobin

type TopicAndPartition =
    { TopicName: TopicName
      PartitionSpecification: PartitionSpecification }

type Partition =
    | Partition of int
    | Any

let determinePartitionID (determinePartitionCount: TopicName -> int option) (topicAndPartition: TopicAndPartition) : Partition =
    match topicAndPartition.PartitionSpecification with
    | ComputeNodeID computeNodeId ->
        match determinePartitionCount topicAndPartition.TopicName with
        | Some count -> Partition (computeNodeId % count)
        | None -> Any
    | PartitionID partitionID ->
        Partition partitionID
    | RoundRobin -> Any

type ProviderSearchRequest<'searchRequest> =
    { RequestID: RequestID
      ResponseTopic: TopicAndPartition
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
