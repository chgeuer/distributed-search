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
    using static Fundamentals.Types;

    public class AzureMessagingClient<TMessagePayload> : IMessageClient<TMessagePayload>
    {
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

        public IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            IObservable<PartitionEvent> partitionEvents = string.IsNullOrEmpty(this.partitionId)
                ? this.consumerClient.CreateObservable(cancellationToken)
                : this.consumerClient.CreateObservable(
                    partitionId: this.partitionId,
                    startingPosition: startingPosition.AsEventPosition(),
                    cancellationToken: cancellationToken);

            return partitionEvents
                .Select(partitionEvent => partitionEvent.Data)
                .Select(eventData => new Message<TMessagePayload>(
                    offset: eventData.Offset,
                    requestID: eventData.Properties.GetRequestID(),
                    payload: eventData.GetBodyAsUTF8().DeserializeJSON<TMessagePayload>()));
        }

        public Task<long> SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default)
            => this.InnerSend(
                messagePayload: messagePayload,
                handleEventData: null,
                cancellationToken: cancellationToken);

        public Task<long> SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default)
            => this.InnerSend(
                messagePayload: messagePayload,
                handleEventData: eventData => eventData.SetRequestID(requestId),
                cancellationToken: cancellationToken);

        private async Task<long> InnerSend(TMessagePayload messagePayload, Action<EventData> handleEventData, CancellationToken cancellationToken)
        {
            using EventDataBatch batchOfOne = await this.producerClient.CreateBatchAsync(cancellationToken);
            var eventData = new EventData(eventBody: messagePayload.AsJSON().ToUTF8Bytes());

            handleEventData?.Invoke(eventData);

            batchOfOne.TryAdd(eventData);
            await this.producerClient.SendAsync(batchOfOne, cancellationToken);

            // TODO Check whether eventData.Offset is set as part of SendAsync
            return eventData.Offset;
        }
    }
}