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
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;

    public class AzureMessagingClient<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly EventHubConsumerClient consumerClient;

        private readonly EventHubProducerClient producerClient;

        private readonly ResponseTopicAddress responseTopicAddress;
        private readonly CreateBatchOptions createBatchOptions;

        public AzureMessagingClient(ResponseTopicAddress responseTopicAddress)
        {
            this.consumerClient = new EventHubConsumerClient(
                consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: responseTopicAddress.TopicName,
                credential: DemoCredential.AADServicePrincipal);

            this.producerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: responseTopicAddress.TopicName,
                credential: DemoCredential.AADServicePrincipal);

            this.responseTopicAddress = responseTopicAddress;

            this.createBatchOptions = FSharpOption<int>.get_IsSome(this.responseTopicAddress.PartitionId)
                  ? new CreateBatchOptions { PartitionId = this.responseTopicAddress.PartitionId.Value.ToString() }
                  : new CreateBatchOptions { };
        }

        public IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            IObservable<PartitionEvent> partitionEvents =
                FSharpOption<int>.get_IsSome(this.responseTopicAddress.PartitionId)
                ? this.consumerClient.CreateObservable(
                    partitionId: this.responseTopicAddress.PartitionId.Value,
                    startingPosition: startingPosition.AsEventPosition(),
                    cancellationToken: cancellationToken)
                : this.consumerClient.CreateObservable(cancellationToken);

            return partitionEvents
                .Select(partitionEvent => partitionEvent.Data)
                .Select(eventData => new Message<TMessagePayload>(
                    offset: UpdateOffset.NewUpdateOffset(eventData.Offset),
                    requestID: eventData.Properties.GetRequestID(),
                    payload: eventData.GetBodyAsUTF8().DeserializeJSON<TMessagePayload>()));
        }

        public Task<UpdateOffset> SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default)
            => this.InnerSend(
                messagePayload: messagePayload,
                handleEventData: null,
                cancellationToken: cancellationToken);

        public Task<UpdateOffset> SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default)
            => this.InnerSend(
                messagePayload: messagePayload,
                handleEventData: eventData => eventData.SetRequestID(requestId),
                cancellationToken: cancellationToken);

        private async Task<UpdateOffset> InnerSend(TMessagePayload messagePayload, Action<EventData> handleEventData, CancellationToken cancellationToken)
        {
            using EventDataBatch batchOfOne = await this.producerClient.CreateBatchAsync(
                options: this.createBatchOptions,
                cancellationToken: cancellationToken);
            var eventData = new EventData(eventBody: messagePayload.AsJSON().ToUTF8Bytes());

            handleEventData?.Invoke(eventData);

            batchOfOne.TryAdd(eventData);
            await this.producerClient.SendAsync(batchOfOne, cancellationToken);

            if (string.IsNullOrEmpty(this.createBatchOptions.PartitionId))
            {
                // if we don't send to a particular partitionID, then we round-robin,
                // and then the client doesn't know where we sent to
                await Console.Out.WriteLineAsync($"Cannot determine offset where we enqueued a {messagePayload.GetType().FullName}");

                return UpdateOffset.NewUpdateOffset(-1);
            }
            else
            {
                // even if we send to a particular partition,
                // we don't get the offset as part of the send operation
                var p = await this.producerClient.GetPartitionPropertiesAsync(
                    partitionId: this.createBatchOptions.PartitionId,
                    cancellationToken: cancellationToken);
                long offset = p.LastEnqueuedOffset;

                await Console.Out.WriteLineAsync($"Enqueued at {offset}");

                return UpdateOffset.NewUpdateOffset(offset);
            }
        }
    }
}