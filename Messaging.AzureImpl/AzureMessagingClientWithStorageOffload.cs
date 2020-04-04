namespace Messaging.AzureImpl
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;
    using static Fundamentals.Types;

    public class AzureMessagingClientWithStorageOffload<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly AzureStorageOffload storageOffload;
        private readonly AzureMessagingClient<StorageOffloadReference> innerClient;

        public AzureMessagingClientWithStorageOffload(AzureMessagingClient<StorageOffloadReference> innerClient, AzureStorageOffload storageOffload)
        {
            this.innerClient = innerClient;
            this.storageOffload = storageOffload;
        }

        public IObservable<Message<TMessagePayload>> CreateObervable(
            SeekPosition startingPosition,
            CancellationToken cancellationToken = default)
            => this.innerClient
                .CreateObervable(
                    startingPosition: startingPosition,
                    cancellationToken: cancellationToken)
                .SelectMany(async message =>
                {
                    var payload = await this.storageOffload.Download<TMessagePayload>(
                        blobName: message.Payload.Address,
                        cancellationToken: cancellationToken);

                    return new Message<TMessagePayload>(
                        offset: message.Offset,
                        payload: payload,
                        properties: message.Properties);
                });

        public async Task SendMessage(
            TMessagePayload messagePayload,
            CancellationToken cancellationToken = default)
        {
            var blobName = $"{Guid.NewGuid()}.json";

            await this.storageOffload.Upload(
                blobName: blobName,
                stream: messagePayload.AsJSONStream(),
                cancellationToken: cancellationToken);

            await this.innerClient.SendMessage(
                messagePayload: new StorageOffloadReference(address: blobName),
                cancellationToken: cancellationToken);
        }

        public async Task SendMessage(
            TMessagePayload messagePayload,
            string requestId,
            CancellationToken cancellationToken = default)
        {
            var blobName = $"{requestId}/{Guid.NewGuid()}.json";

            await this.storageOffload.Upload(
                blobName: blobName,
                stream: messagePayload.AsJSONStream(),
                cancellationToken: cancellationToken);

            await this.innerClient.SendMessage(
                messagePayload: new StorageOffloadReference(address: blobName),
                requestId: requestId,
                cancellationToken: cancellationToken);
        }
    }
}