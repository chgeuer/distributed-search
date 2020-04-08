﻿namespace Messaging
{
    using System;
    using Azure.Storage.Blobs;
    using Credentials;
    using Interfaces;
    using Messaging.KafkaImpl;
    using static Fundamentals.Types;

    public static class MessagingClients
    {
        public static IMessageClient<T> Updates<T>(int? partitionId = null)
            => Create<T>(new TopicPartitionID(
                topicName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: partitionId));

        public static IMessageClient<T> Requests<T>(int? partitionId = null)
            => Create<T>(new TopicPartitionID(
                topicName: DemoCredential.EventHubTopicNameRequests,
                partitionId: partitionId));

        public static IMessageClient<T> Responses<T>(string responseTopicName, int partitionId)
            => Create<T>(new TopicPartitionID(topicName: responseTopicName, partitionId: partitionId));

        public static IMessageClient<T> WithStorageOffload<T>(TopicPartitionID topicPartitionID, string accountName, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/"),
                credential: DemoCredential.AADServicePrincipal);

            return new MessagingClientWithStorageOffload<T>(
                innerClient: Create<StorageOffloadReference>(topicPartitionID: topicPartitionID),
                storageOffload: new StorageOffload(blobContainerClient.UpAndDownloadLambdas()));
        }

        private static StorageOffloadFunctions UpAndDownloadLambdas(
           this BlobContainerClient containerClient)
           => new StorageOffloadFunctions(
               upload: containerClient.UploadBlobAsync,
               download: async (blobName, cancellationToken) =>
               {
                   var blobClient = containerClient.GetBlobClient(blobName: blobName);
                   var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                   return result.Value.Content;
               });

        private static IMessageClient<T> Create<T>(TopicPartitionID topicPartitionID)
            => new KafkaMessagingClient<T>(topicPartitionID: topicPartitionID) as IMessageClient<T>;
    }
}