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

type TopicName = string

//type PartitionSpecification =
//    | PartitionID of int
//    | ComputeNodeID of int
//    | RoundRobin

type TopicAndComputeNodeID =
    { TopicName: TopicName
      /// If a node expects responses in a particular partition,
      /// specify a node ID number here. This node ID will be used
      /// modulo the number of partitions, to determine the
      /// precise partition.
      ComputeNodeId: int option }

type Partition =
    | Partition of int
    | Any

let determinePartitionID (determinePartitionCount: TopicName -> int option) (topicAndComputeNodeID: TopicAndComputeNodeID) : Partition =
    match topicAndComputeNodeID.ComputeNodeId with
    | Some computeNodeId ->
        match determinePartitionCount topicAndComputeNodeID.TopicName with
        | Some count -> Partition (computeNodeId % count)
        | None -> Any
    | None -> Any

type ProviderSearchRequest<'searchRequest> =
    { RequestID: RequestID
      ResponseTopic: TopicAndComputeNodeID
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
