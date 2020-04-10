namespace Mercury.Messaging
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Mercury.Interfaces;
    using Mercury.Utils;
    using Mercury.Utils.Extensions;
    using static Fundamentals.Types;

    public class MessagingClientWithStorageOffload<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly StorageOffload storageOffload;
        private readonly IMessageClient<StorageOffloadReference> innerClient;

        public MessagingClientWithStorageOffload(IMessageClient<StorageOffloadReference> innerClient, StorageOffload storageOffload)
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
                        requestID: message.RequestID,
                        payload: payload);
                });

        public async Task<Offset> SendMessage(
            TMessagePayload messagePayload,
            CancellationToken cancellationToken = default)
        {
            var blobName = $"{Guid.NewGuid()}.json";

            await this.storageOffload.Upload(
                blobName: blobName,
                stream: messagePayload.AsJSONStream(),
                cancellationToken: cancellationToken);

            return await this.innerClient.SendMessage(
                messagePayload: new StorageOffloadReference(address: blobName),
                cancellationToken: cancellationToken);
        }

        public async Task<Offset> SendMessage(
            TMessagePayload messagePayload,
            string requestId,
            CancellationToken cancellationToken = default)
        {
            var blobName = $"{requestId}/{Guid.NewGuid()}.json";

            await this.storageOffload.Upload(
                blobName: blobName,
                stream: messagePayload.AsJSONStream(),
                cancellationToken: cancellationToken);

            return await this.innerClient.SendMessage(
                messagePayload: new StorageOffloadReference(address: blobName),
                requestId: requestId,
                cancellationToken: cancellationToken);
        }
    }
}