namespace Messaging.AzureImpl
{
    using Azure.Storage.Blobs;
    using static Fundamentals.Types;

    public static class AzureBlobExtensions
    {
        public static StorageOffloadFunctions UpAndDownloadLambdas(
            this BlobContainerClient containerClient)
            => new StorageOffloadFunctions(
                upload: containerClient.UploadBlobAsync,
                download: async (blobName, cancellationToken) =>
                {
                    var blobClient = containerClient.GetBlobClient(blobName: blobName);
                    var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                    return result.Value.Content;
                });
    }
}