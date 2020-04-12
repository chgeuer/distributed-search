namespace Mercury.Messaging
{
    using Mercury.Interfaces;
    using Mercury.Utils;
    using static Fundamentals.Types;

    public static class MessagingClients
    {
        public static IMessageClient<T> Updates<T>(IDistributedSearchConfiguration demoCredential, int? computeNodeId = null)
            => Create<T>(
                demoCredential: demoCredential,
                topicAndComputeNodeID: new TopicAndComputeNodeID(
                    topicName: demoCredential.EventHubTopicNameBusinessDataUpdates,
                    computeNodeId: computeNodeId));

        public static IMessageClient<T> Requests<T>(IDistributedSearchConfiguration demoCredential, int? computeNodeId = null)
            => Create<T>(
                demoCredential: demoCredential,
                topicAndComputeNodeID: new TopicAndComputeNodeID(
                    topicName: demoCredential.EventHubTopicNameRequests,
                    computeNodeId: computeNodeId));

        public static IMessageClient<T> Responses<T>(IDistributedSearchConfiguration demoCredential, string responseTopicName, int computeNodeId)
            => Create<T>(demoCredential: demoCredential, new TopicAndComputeNodeID(topicName: responseTopicName, computeNodeId: computeNodeId));

        public static IMessageClient<T> WithStorageOffload<T>(IDistributedSearchConfiguration demoCredential, TopicAndComputeNodeID topicAndComputeNodeID, StorageOffload storageOffload)
        {
            return new MessagingClientWithStorageOffload<T>(
                innerClient: Create<StorageOffloadReference>(demoCredential: demoCredential, topicAndComputeNodeID: topicAndComputeNodeID),
                storageOffload: storageOffload);
        }

        private static IMessageClient<T> Create<T>(IDistributedSearchConfiguration demoCredential, TopicAndComputeNodeID topicAndComputeNodeID)
            => new KafkaMessagingClient<T>(demoCredential: demoCredential, topicAndComputeNodeID: topicAndComputeNodeID) as IMessageClient<T>;
    }
}