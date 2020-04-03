namespace Messaging.AzureImpl
{
    using Credentials;
    using Interfaces;

    public static class MessagingClients
    {
        public static AzureMessagingClient<TResponses> Updates<TResponses>(string partitionId)
            => new AzureMessagingClient<TResponses>(
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: partitionId);

        public static AzureMessagingClient<TRequests> Requests<TRequests>(string partitionId)
            => new AzureMessagingClient<TRequests>(
                eventHubName: DemoCredential.EventHubTopicNameRequests,
                partitionId: partitionId);

        public static AzureMessagingClient<TResponses> Responses<TResponses>(string topicName, string partitionId)
            => new AzureMessagingClient<TResponses>(
                eventHubName: topicName,
                partitionId: partitionId);

        public static AzureMessagingClientWithStorageOffload<TPayload> WithStorageOffload<TPayload>(string topicName, string partitionId, string accountName, string containerName)
        {
            return new AzureMessagingClientWithStorageOffload<TPayload>(
                innerClient: new AzureMessagingClient<StorageOffloadReference>(
                    eventHubName: topicName,
                    partitionId: partitionId),
                storageOffload: new StorageOffload(
                    accountName: accountName,
                    containerName: containerName));
        }
    }
}