namespace Messaging.KafkaImpl
{
    using System;
    using Messaging.AzureImpl;
    using Azure.Storage.Blobs;
    using Credentials;
    using Interfaces;
    using static Fundamentals.Types;

    public static class KafkaMessagingClients
    {
        public static IMessageClient<TResponses> Updates<TResponses>(string partitionId)
            => new KafkaMessagingClient<TResponses>(
                topic: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: partitionId);

        public static IMessageClient<TRequests> Requests<TRequests>(string partitionId)
           => new KafkaMessagingClient<TRequests>(
               topic: DemoCredential.EventHubTopicNameRequests,
               partitionId: partitionId);

        public static IMessageClient<TResponses> Responses<TResponses>(string topicName, string partitionId)
            => new KafkaMessagingClient<TResponses>(
                topic: topicName,
                partitionId: partitionId);

        public static IMessageClient<TPayload> WithStorageOffload<TPayload>(string topicName, string partitionId, string accountName, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/"),
                credential: DemoCredential.AADServicePrincipal);

            return new MessagingClientWithStorageOffload<TPayload>(
                innerClient: new KafkaMessagingClient<StorageOffloadReference>(
                    topic: topicName, partitionId: partitionId),
                storageOffload: new StorageOffload(
                    blobContainerClient.UpAndDownloadLambdas()));
        }
    }
}