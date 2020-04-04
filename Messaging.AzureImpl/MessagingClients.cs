namespace Messaging.AzureImpl
{
    using System.Collections.Generic;
    using Azure.Messaging.EventHubs;
    using Credentials;
    using Interfaces;
    using static Fundamentals.Types;

    public static class MessagingClients
    {
        public static IMessageClient<TResponses> Updates<TResponses>(string partitionId)
            => new AzureMessagingClient<TResponses>(
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: partitionId);

        public static IMessageClient<TRequests> Requests<TRequests>(string partitionId)
            => new AzureMessagingClient<TRequests>(
                eventHubName: DemoCredential.EventHubTopicNameRequests,
                partitionId: partitionId);

        public static IMessageClient<TResponses> Responses<TResponses>(string topicName, string partitionId)
            => new AzureMessagingClient<TResponses>(
                eventHubName: topicName,
                partitionId: partitionId);

        public static IMessageClient<TPayload> WithStorageOffload<TPayload>(string topicName, string partitionId, string accountName, string containerName)
        {
            return new AzureMessagingClientWithStorageOffload<TPayload>(
                innerClient: new AzureMessagingClient<StorageOffloadReference>(
                    eventHubName: topicName,
                    partitionId: partitionId),
                storageOffload: new AzureStorageOffload(
                    accountName: accountName,
                    containerName: containerName));
        }

        public static readonly string RequestIdPropertyName = "requestIDString";

        public static string GetRequestID(this IDictionary<string, object> properties)
            => properties[RequestIdPropertyName] as string;

        public static void SetRequestID(this EventData eventData, string requestId) =>
            eventData.Properties.Add(RequestIdPropertyName, requestId);
    }
}