namespace Messaging.AzureImpl
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;

    public static class AzureBlobExtensions
    {
        public static Func<string, Stream, CancellationToken, Task> UploadLambda(
            this BlobContainerClient containerClient)
                => containerClient.UploadBlobAsync;

        public static Func<string, CancellationToken, Task<Stream>> DownloadLambda(
            this BlobContainerClient containerClient)
            => async (blobName, cancellationToken) =>
            {
                var blobClient = containerClient.GetBlobClient(blobName: blobName);
                var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                return result.Value.Content;
            };
    }
}