namespace Mercury.Utils
{
    using System.Threading;
    using System.Threading.Tasks;
    using Mercury.Utils.Extensions;
    using static Fundamentals.Types;

    /// <summary>
    /// Handles upload and download of .NET types as JSON messages.
    /// </summary>
    public class StorageOffload
    {
        private readonly StorageOffloadFunctions storageOffloadFunctions;

        public StorageOffload(StorageOffloadFunctions storageOffloadFunctions)
        {
            this.storageOffloadFunctions = storageOffloadFunctions;
        }

        public Task Upload<T>(string blobName, T value, CancellationToken cancellationToken)
            => this.storageOffloadFunctions.Upload(
                GetFilename(blobName),
                value
                    .AsJSONStream()
                    .GZipCompress(),
                cancellationToken);

        public async Task<T> Download<T>(string blobName, CancellationToken cancellationToken)
        {
            var stream = await this.storageOffloadFunctions.Download(
                GetFilename(blobName),
                cancellationToken);

            return await stream
                .GZipDecompress()
                .ReadJSON<T>();
        }

        private static string GetFilename(string blobName)
            => $"{blobName}.json.gz";
    }
}