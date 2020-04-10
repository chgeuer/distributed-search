namespace Mercury.Utils.Extensions
{
    using Azure.Storage.Blobs;
    using static Mercury.Fundamentals.Types;

    public static class AzureBlobStorageExtensions
    {
        public static StorageOffload ToStorageOffload(this BlobContainerClient containerClient)
        {
            return new StorageOffload(
                new StorageOffloadFunctions(
                upload: containerClient.UploadBlobAsync,
                download: async (blobName, cancellationToken) =>
                {
                    var blobClient = containerClient.GetBlobClient(blobName: blobName);
                    var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                    return result.Value.Content;
                }));
        }
    }
}