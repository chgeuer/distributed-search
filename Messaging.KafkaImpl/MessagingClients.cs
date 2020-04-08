namespace Messaging
{
    using System;
    using Azure.Storage.Blobs;
    using Interfaces;
    using Messaging.KafkaImpl;
    using static Fundamentals.Types;

    public static class MessagingClients
    {
        public static IMessageClient<T> Updates<T>(IDistributedSearchConfiguration demoCredential, int? partitionId = null)
            => Create<T>(
                demoCredential: demoCredential,
                topicPartitionID: new TopicPartitionID(
                    topicName: demoCredential.EventHubTopicNameBusinessDataUpdates,
                    partitionId: partitionId));

        public static IMessageClient<T> Requests<T>(IDistributedSearchConfiguration demoCredential, int? partitionId = null)
            => Create<T>(
                demoCredential: demoCredential,
                topicPartitionID: new TopicPartitionID(
                    topicName: demoCredential.EventHubTopicNameRequests,
                    partitionId: partitionId));

        public static IMessageClient<T> Responses<T>(IDistributedSearchConfiguration demoCredential, string responseTopicName, int partitionId)
            => Create<T>(demoCredential: demoCredential, new TopicPartitionID(topicName: responseTopicName, partitionId: partitionId));

        public static IMessageClient<T> WithStorageOffload<T>(IDistributedSearchConfiguration demoCredential, TopicPartitionID topicPartitionID, string accountName, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/"),
                credential: demoCredential.AADServicePrincipal);

            return new MessagingClientWithStorageOffload<T>(
                innerClient: Create<StorageOffloadReference>(demoCredential: demoCredential, topicPartitionID: topicPartitionID),
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

        private static IMessageClient<T> Create<T>(IDistributedSearchConfiguration demoCredential, TopicPartitionID topicPartitionID)
            => new KafkaMessagingClient<T>(demoCredential: demoCredential, topicPartitionID: topicPartitionID) as IMessageClient<T>;
    }
}
