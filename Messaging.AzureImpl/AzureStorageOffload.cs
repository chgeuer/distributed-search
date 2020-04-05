namespace Messaging.AzureImpl
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Credentials;
    using Interfaces;

    public class AzureStorageOffload
    {
        private readonly BlobContainerClient blobContainerClient;

        public AzureStorageOffload(string accountName, string containerName)
        {
            this.blobContainerClient = new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/"),
                    credential: DemoCredential.AADServicePrincipal);
        }

        public async Task Upload(string blobName, Stream stream, CancellationToken cancellationToken)
        {
            await this.blobContainerClient.UploadBlobAsync(
                blobName: blobName, content: stream, cancellationToken: cancellationToken);
        }

        public async Task<T> Download<T>(string blobName, CancellationToken cancellationToken)
        {
            var blobClient = this.blobContainerClient.GetBlobClient(blobName: blobName);
            var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
            return await result.Value.Content.ReadJSON<T>();
        }
    }
}
