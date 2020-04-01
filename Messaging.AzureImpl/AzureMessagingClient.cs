namespace Messaging.AzureImpl
{
    using System;
    using System.Collections.Generic;
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
        public readonly string RequestIdPropertyName = "requestIDString";

        private readonly EventHubConsumerClient consumerClient;

        private readonly EventHubProducerClient producerClient;

        private readonly string partitionId;

        public AzureMessagingClient(string eventHubName, string partitionId)
        {
            this.consumerClient = new EventHubConsumerClient(
                consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: eventHubName,
                credential: DemoCredential.AADServicePrincipal);

            this.producerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: eventHubName,
                credential: DemoCredential.AADServicePrincipal);

            this.partitionId = partitionId;
        }

        public IObservable<Message<T>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            IObservable<PartitionEvent> partitionEvents = this.partitionId == null
                ? this.consumerClient.CreateObservable(cancellationToken)
                : this.consumerClient.CreateObservable(this.partitionId, startingPosition.AsEventPosition(), cancellationToken);

            return partitionEvents
                .Select(partitionEvent => partitionEvent.Data)
                .Select(eventData => new Message<T>(
                    offset: eventData.Offset,
                    value: eventData.GetBodyAsUTF8().DeserializeJSON<T>(),
                    properties: eventData.Properties));
        }

        public Task SendMessage(T t) => this.InnerSend(t);

        public Task SendMessage(T t, string requestId) => this.InnerSend(t, eventData => SetRequestID(eventData, requestId));

        public string GetRequestID(IDictionary<string, object> properties)
            => properties[this.RequestIdPropertyName] as string;

        private void SetRequestID(EventData eventData, string requestId) =>
            eventData.Properties.Add(this.RequestIdPropertyName, requestId);

        private async Task InnerSend(T t, Action<EventData> handleEventData = null)
        {
            using EventDataBatch batchOfOne = await this.producerClient.CreateBatchAsync();
            var eventData = new EventData(eventBody: t.AsJSON().ToUTF8Bytes());

            handleEventData?.Invoke(eventData);

            batchOfOne.TryAdd(eventData);
            await this.producerClient.SendAsync(batchOfOne);
        }
    }
}