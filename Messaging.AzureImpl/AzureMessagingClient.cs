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
            IObservable<PartitionEvent> partitionEvents = this.partitionId == null
                ? this.ConsumerClient.CreateObservable(cancellationToken)
                : this.ConsumerClient.CreateObservable(this.partitionId, startingPosition.AsEventPosition(), cancellationToken);

            return partitionEvents
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

            handleEventData?.Invoke(eventData);

            batchOfOne.TryAdd(eventData);
            await this.ProducerClient.SendAsync(batchOfOne);
        }
    }
}