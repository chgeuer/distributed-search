namespace Mercury.Messaging;

using Mercury.Interfaces;
using static Fundamentals.Types;

public static class MessagingClients
{
    public static IWatermarkMessageClient<T> Updates<T>(IDistributedSearchConfiguration demoCredential)
        => new KafkaMessagingClient<T>(
            demoCredential: demoCredential,
            topicAndPartition: new TopicAndPartition(
                topicName: demoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionSpecification: PartitionSpecification.NewPartitionID(0)));

    public static IRequestResponseMessageClient<T> Requests<T>(IDistributedSearchConfiguration demoCredential)
        => new ServiceBusMessagingClient<T>(demoCredential: demoCredential);

    public static IRequestResponseMessageClient<T> Responses<T>(IDistributedSearchConfiguration demoCredential, TopicAndPartition topicAndPartition)
        => new KafkaMessagingClient<T>(demoCredential: demoCredential, topicAndPartition: topicAndPartition);
}