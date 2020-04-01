namespace Messaging.AzureImpl
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;

    public class AzureMessagingClientWithStorageOffload<TMessage, TPayload>
        where TMessage : IMessageEnrichableWithPayloadAddress
    {
        private readonly AzureMessagingClient<TMessage> innerClient;
        private readonly StorageOffload storageOffload;

        public AzureMessagingClientWithStorageOffload(AzureMessagingClient<TMessage> innerClient, StorageOffload storageOffload)
        {
            this.innerClient = innerClient;
            this.storageOffload = storageOffload;
        }

        public async Task Send(TMessage message, TPayload payload, string requestId, string blobName, CancellationToken cancellationToken = default)
        {
            await this.storageOffload.Upload(
                blobName: blobName,
                stream: payload.AsJSONStream(),
                cancellationToken: cancellationToken);

            var annotatedMessage = (TMessage)message.SetPayloadAddress(address: blobName);

            await this.innerClient.SendMessageWithRequestID(annotatedMessage, requestId);
        }

        public IObservable<Message<TPayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            return this.innerClient
                .CreateObervable(startingPosition: startingPosition, cancellationToken: cancellationToken)
                .SelectMany(async message =>
                {
                    var searchResponse = message.Value;
                    var address = searchResponse.GetPayloadAddress();

                    var payload = await this.storageOffload.Download<TPayload>(
                        blobName: address,
                        cancellationToken: cancellationToken);

                    return new Message<TPayload>(
                        offset: message.Offset,
                        value: payload,
                        properties: message.Properties);
                });
        }
    }
}