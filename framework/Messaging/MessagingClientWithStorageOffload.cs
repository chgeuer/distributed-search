namespace Mercury.Messaging
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Mercury.Interfaces;
    using Mercury.Utils;
    using static Fundamentals.Types;
    using static Fundamentals.Types.BlobStorageAddressModule;

    public class MessagingClientWithStorageOffload<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly StorageOffload storageOffload;
        private readonly IMessageClient<StorageOffloadReference> innerClient;

        public MessagingClientWithStorageOffload(IMessageClient<StorageOffloadReference> innerClient, StorageOffload storageOffload)
        {
            this.innerClient = innerClient;
            this.storageOffload = storageOffload;
        }

        public IObservable<WatermarkMessage<TMessagePayload>> CreateObervable(
            SeekPosition startingPosition,
            CancellationToken cancellationToken = default)
            => this.innerClient
                .CreateObervable(
                    startingPosition: startingPosition,
                    cancellationToken: cancellationToken)
                .SelectMany(async message =>
                {
                    var payload = await this.storageOffload.Download<TMessagePayload>(
                        blobName: value(message.Payload.Address),
                        cancellationToken: cancellationToken);

                    return new WatermarkMessage<TMessagePayload>(
                        watermark: message.Watermark,
                        requestID: message.RequestID,
                        payload: payload);
                });

        public async Task<Watermark> SendMessage(
            TMessagePayload messagePayload,
            CancellationToken cancellationToken = default)
        {
            var blobName = Guid.NewGuid().ToString();

            await this.storageOffload.Upload(
                blobName: blobName,
                value: messagePayload,
                cancellationToken: cancellationToken);

            return await this.innerClient.SendMessage(
                messagePayload: new StorageOffloadReference(address: create(blobName)),
                cancellationToken: cancellationToken);
        }

        public async Task<Watermark> SendMessage(
            TMessagePayload messagePayload,
            string requestId,
            CancellationToken cancellationToken = default)
        {
            var blobName = $"{requestId}/{Guid.NewGuid()}";

            await this.storageOffload.Upload(
                blobName: blobName,
                value: messagePayload,
                cancellationToken: cancellationToken);

            return await this.innerClient.SendMessage(
                messagePayload: new StorageOffloadReference(address: create(blobName)),
                requestId: requestId,
                cancellationToken: cancellationToken);
        }
    }
}