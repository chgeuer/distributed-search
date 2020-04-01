namespace Messaging.AzureImpl
{
    using Credentials;

    public static class MessagingClients
    {
        public static AzureMessagingClient<TRequests> Requests<TRequests>()
        {
            return new AzureMessagingClient<TRequests>(
                eventHubName: DemoCredential.EventHubTopicNameRequests,
                partitionId: null);
        }

        public static AzureMessagingClient<TResponses> Responses<TResponses>(string partitionId)
        {
            return new AzureMessagingClient<TResponses>(
                eventHubName: DemoCredential.EventHubTopicNameResponses,
                partitionId: partitionId);
        }

        public static AzureMessagingClient<TUpdates> Updates<TUpdates>(string partitionId)
        {
            return new AzureMessagingClient<TUpdates>(
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: partitionId);
        }
    }
}
