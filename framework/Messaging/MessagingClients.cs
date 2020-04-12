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
                topicPartitionID: new TopicPartitionID(
                    topicName: demoCredential.EventHubTopicNameBusinessDataUpdates,
                    computeNodeId: computeNodeId));

        public static IMessageClient<T> Requests<T>(IDistributedSearchConfiguration demoCredential, int? computeNodeId = null)
            => Create<T>(
                demoCredential: demoCredential,
                topicPartitionID: new TopicPartitionID(
                    topicName: demoCredential.EventHubTopicNameRequests,
                    computeNodeId: computeNodeId));

        public static IMessageClient<T> Responses<T>(IDistributedSearchConfiguration demoCredential, string responseTopicName, int computeNodeId)
            => Create<T>(demoCredential: demoCredential, new TopicPartitionID(topicName: responseTopicName, computeNodeId: computeNodeId));

        public static IMessageClient<T> WithStorageOffload<T>(IDistributedSearchConfiguration demoCredential, TopicPartitionID topicPartitionID, StorageOffload storageOffload)
        {
            return new MessagingClientWithStorageOffload<T>(
                innerClient: Create<StorageOffloadReference>(demoCredential: demoCredential, topicPartitionID: topicPartitionID),
                storageOffload: storageOffload);
        }

        private static IMessageClient<T> Create<T>(IDistributedSearchConfiguration demoCredential, TopicPartitionID topicPartitionID)
            => new KafkaMessagingClient<T>(demoCredential: demoCredential, topicPartitionID: topicPartitionID) as IMessageClient<T>;
    }
}