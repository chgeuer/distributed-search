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
    using static Fundamentals.Types;
    using Interfaces;
    using Microsoft.FSharp.Collections;

    public class AzureMessagingClient<TMessagePayload>
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
            IObservable<PartitionEvent> partitionEvents = this.partitionId == null
                ? this.consumerClient.CreateObservable(cancellationToken)
                : this.consumerClient.CreateObservable(this.partitionId, startingPosition.AsEventPosition(), cancellationToken);

            return partitionEvents
                .Select(partitionEvent => partitionEvent.Data)
                .Select(eventData => new Message<TMessagePayload>(
                    offset: eventData.Offset,
                    value: eventData.GetBodyAsUTF8().DeserializeJSON<TMessagePayload>(),
                    properties: new FSharpMap<string, object>(eventData.Properties.Select(
                        kvp => Tuple.Create(kvp.Key, kvp.Value)).ToArray())));
        }

        public Task SendMessage(TMessagePayload value)
            => this.InnerSend(value: value, handleEventData: null);

        public Task SendMessageWithRequestID(TMessagePayload value, string requestId)
            => this.InnerSend(value: value, handleEventData: eventData => eventData.SetRequestID(requestId));

        private async Task InnerSend(TMessagePayload value, Action<EventData> handleEventData)
        {
            using EventDataBatch batchOfOne = await this.producerClient.CreateBatchAsync();
            var eventData = new EventData(eventBody: value.AsJSON().ToUTF8Bytes());

            handleEventData?.Invoke(eventData);

            batchOfOne.TryAdd(eventData);
            await this.producerClient.SendAsync(batchOfOne);
        }
    }
}