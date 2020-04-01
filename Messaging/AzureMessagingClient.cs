namespace Messaging.AzureImpl
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Producer;
    using Credentials;
    using Interfaces;

    public static class MessagingClients
    {
        public static AzureMessagingClient<TRequests> Requests<TRequests>()
        {
            return new AzureMessagingClient<TRequests>(
                eventHubName: DemoCredential.EventHubTopicNameRequests,
                partitionId: "0");
        }

        public static AzureMessagingClient<TResponses> Responses<TResponses>()
        {
            return new AzureMessagingClient<TResponses>(
                eventHubName: DemoCredential.EventHubTopicNameResponses,
                partitionId: "0");
        }

        public static AzureMessagingClient<TUpdates> Updates<TUpdates>()
        {
            return new AzureMessagingClient<TUpdates>(
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                partitionId: "0");
        }
    }

    public class SeekPosition
    {
        private bool fromTail;
        private long fromPosition;

        public static SeekPosition Tail { get; } = new SeekPosition { fromTail = true, fromPosition = 0 };

        public static SeekPosition FromPosition(long position) => new SeekPosition { fromTail = false, fromPosition = position };

        private SeekPosition()
        {
        }

        internal EventPosition AsEventPosition() => this.fromTail
                ? EventPosition.Latest
                : EventPosition.FromOffset(
                    offset: this.fromPosition,
                    isInclusive: false);
    }

    public class AzureMessagingClient<T>
    {
        public EventHubConsumerClient ConsumerClient { get; private set; }

        public EventHubProducerClient ProducerClient { get; private set; }

        private string partitionId;

        public AzureMessagingClient(string eventHubName, string partitionId)
        {
            this.ConsumerClient = new EventHubConsumerClient(
                consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: eventHubName,
                credential: DemoCredential.AADServicePrincipal);
            this.ProducerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: eventHubName,
                credential: DemoCredential.AADServicePrincipal);

            this.partitionId = partitionId;
        }

        public IObservable<Tuple<long, T>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            return this.ConsumerClient
                .CreateObservable(
                    partitionId: this.partitionId,
                    startingPosition: startingPosition.AsEventPosition(),
                    cancellationToken: cancellationToken)
                .Select(partitionEvent => partitionEvent.Data)
                .Select(eventData => new { eventData.Offset, JsonString = eventData.GetBodyAsUTF8() })
                .Select(x => Tuple.Create(x.Offset, x.JsonString.DeserializeJSON<T>()));
        }

        public Task SendMessage(T t) => this.InnerSend(t);

        public Task SendMessage(T t, string requestId) => this.InnerSend(t, eventData => eventData.Properties.Add("requestIDString", requestId));

        private async Task InnerSend(T t, Action<EventData> handleEventData = null)
        {
            using EventDataBatch batchOfOne = await this.ProducerClient.CreateBatchAsync();
            var eventData = new EventData(eventBody: t.AsJSON().ToUTF8Bytes());

            if (handleEventData != null)
            {
                handleEventData(eventData);
            }

            batchOfOne.TryAdd(eventData);
            await this.ProducerClient.SendAsync(batchOfOne);
        }
    }
}