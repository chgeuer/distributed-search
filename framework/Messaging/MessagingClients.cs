namespace Mercury.Messaging
{
    using Mercury.Interfaces;
    using Mercury.Utils;
    using static Fundamentals.Types;

    public static class MessagingClients
    {
        public static IMessageClient<T> Updates<T>(IDistributedSearchConfiguration demoCredential)
            => Create<T>(
                demoCredential: demoCredential,
                topicAndPartition: new TopicAndPartition(
                    topicName: demoCredential.EventHubTopicNameBusinessDataUpdates,
                    partitionSpecification: PartitionSpecification.NewPartitionID(0)));

        public static IMessageClient<T> Requests<T>(IDistributedSearchConfiguration demoCredential)
            => Create<T>(
                demoCredential: demoCredential,
                topicAndPartition: new TopicAndPartition(
                    topicName: demoCredential.EventHubTopicNameRequests,
                    partitionSpecification: PartitionSpecification.NewPartitionID(0)));

        public static IMessageClient<T> Responses<T>(IDistributedSearchConfiguration demoCredential, string responseTopicName, int computeNodeId)
            => Create<T>(
                demoCredential: demoCredential,
                topicAndPartition: new TopicAndPartition(
                    topicName: responseTopicName,
                    partitionSpecification: PartitionSpecification.NewComputeNodeID(computeNodeId)));

        public static IMessageClient<T> WithStorageOffload<T>(IDistributedSearchConfiguration demoCredential, TopicAndPartition topicAndPartition, StorageOffload storageOffload)
        {
            return new MessagingClientWithStorageOffload<T>(
                innerClient: Create<StorageOffloadReference>(demoCredential: demoCredential, topicAndPartition: topicAndPartition),
                storageOffload: storageOffload);
        }

        private static IMessageClient<T> Create<T>(IDistributedSearchConfiguration demoCredential, TopicAndPartition topicAndPartition)
            => new KafkaMessagingClient<T>(demoCredential: demoCredential, topicAndPartition: topicAndPartition) as IMessageClient<T>;
    }
}