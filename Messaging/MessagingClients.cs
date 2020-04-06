namespace Messaging
{
    using System;
    using Azure.Storage.Blobs;
    using Credentials;
    using Interfaces;
    using Messaging.AzureImpl;
    using Messaging.KafkaImpl;
    using static Fundamentals.Types;

    public static class MessagingClients
    {
        internal enum Provider { Kafka, EventHub }

        const Provider Impl = Provider.EventHub;

        public static IMessageClient<T> Updates<T>(string partitionId)
            => Create<T>(topicName: DemoCredential.EventHubTopicNameBusinessDataUpdates, partitionId: partitionId);

        public static IMessageClient<T> Requests<T>(string partitionId)
            => Create<T>(topicName: DemoCredential.EventHubTopicNameRequests, partitionId: partitionId);

        public static IMessageClient<T> Responses<T>(string responseTopicName, string partitionId)
            => Create<T>(topicName: responseTopicName, partitionId: partitionId);

        public static IMessageClient<T> WithStorageOffload<T>(string topicName, string partitionId, string accountName, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/"),
                credential: DemoCredential.AADServicePrincipal);

            return new MessagingClientWithStorageOffload<T>(
                innerClient: Create<StorageOffloadReference>(topicName: topicName, partitionId: partitionId),
                storageOffload: new StorageOffload(blobContainerClient.UpAndDownloadLambdas()));
        }

        private static IMessageClient<T> Create<T>(string topicName, string partitionId)
            => Impl == Provider.Kafka
                ? new KafkaMessagingClient<T>(topic: topicName, partitionId: partitionId) as IMessageClient<T>
                : new AzureMessagingClient<T>(eventHubName: topicName, partitionId: partitionId) as IMessageClient<T>;

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
    }
}
