namespace Messaging.AzureImpl
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Fundamentals;
    using Interfaces;

    public class AzureMessagingClientWithStorageOffload<TPayload>
    {
        private readonly AzureMessagingClient<StorageOffloadReference> innerClient;
        private readonly StorageOffload storageOffload;

        public AzureMessagingClientWithStorageOffload(AzureMessagingClient<StorageOffloadReference> innerClient, StorageOffload storageOffload)
        {
            this.innerClient = innerClient;
            this.storageOffload = storageOffload;
        }

        public async Task Send(TPayload payload, string requestId, string blobName, CancellationToken cancellationToken = default)
        {
            await this.storageOffload.Upload(
                blobName: blobName,
                stream: payload.AsJSONStream(),
                cancellationToken: cancellationToken);

            await this.innerClient.SendMessageWithRequestID(
                value: new StorageOffloadReference(requestID: requestId, address: blobName),
                requestId: requestId);
        }

        public IObservable<Message<TPayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            return this.innerClient
                .CreateObervable(startingPosition: startingPosition, cancellationToken: cancellationToken)
                .SelectMany(async message =>
                {
                    var searchResponse = message.Value;

                    var payload = await this.storageOffload.Download<TPayload>(
                        blobName: searchResponse.Address,
                        cancellationToken: cancellationToken);

                    return new Message<TPayload>(
                        offset: message.Offset,
                        value: payload,
                        properties: message.Properties);
                });
        }
    }
}