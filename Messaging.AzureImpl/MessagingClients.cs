namespace Messaging.AzureImpl
{
    using Credentials;

    public static class MessagingClients
    {
        public static AzureMessagingClient<TRequests> Requests<TRequests>(string partitionId)
            => new AzureMessagingClient<TRequests>(
                eventHubName: DemoCredential.EventHubTopicNameRequests,
                partitionId: partitionId);

        public static AzureMessagingClient<TResponses> Responses<TResponses>(string topicName, string partitionId)
            => new AzureMessagingClient<TResponses>(
                eventHubName: topicName,
                partitionId: partitionId);

        public static AzureMessagingClient<TUpdates> Updates<TUpdates>(string partitionId)
            => new AzureMessagingClient<TUpdates>(
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: partitionId);
    }
}