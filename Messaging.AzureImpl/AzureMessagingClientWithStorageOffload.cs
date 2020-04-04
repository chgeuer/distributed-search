namespace Messaging.AzureImpl
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;
    using static Fundamentals.Types;

    public class AzureMessagingClientWithStorageOffload<TMessagePayload>
    {
        private readonly AzureMessagingClient<StorageOffloadReference> innerClient;
        private readonly AzureStorageOffload storageOffload;

        public AzureMessagingClientWithStorageOffload(AzureMessagingClient<StorageOffloadReference> innerClient, AzureStorageOffload storageOffload)
        {
            this.innerClient = innerClient;
            this.storageOffload = storageOffload;
        }

        public async Task Send(TMessagePayload message, string requestId, string blobName, CancellationToken cancellationToken = default)
        {
            await this.storageOffload.Upload(
                blobName: blobName,
                stream: message.AsJSONStream(),
                cancellationToken: cancellationToken);

            await this.innerClient.SendMessageWithRequestID(
                value: new StorageOffloadReference(requestID: requestId, address: blobName),
                requestId: requestId);
        }

        public IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
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
    }
}