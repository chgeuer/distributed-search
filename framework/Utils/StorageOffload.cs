namespace Mercury.Utils
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Mercury.Utils.Extensions;
    using static Fundamentals.Types;

    public class StorageOffload
    {
        private readonly StorageOffloadFunctions storageOffloadFunctions;

        public StorageOffload(StorageOffloadFunctions storageOffloadFunctions)
        {
            this.storageOffloadFunctions = storageOffloadFunctions;
        }

        public Task Upload(string blobName, Stream stream, CancellationToken cancellationToken)
            => this.storageOffloadFunctions.Upload(blobName, stream, cancellationToken);

        public async Task<T> Download<T>(string blobName, CancellationToken cancellationToken)
        {
            var stream = await this.storageOffloadFunctions.Download(
                blobName, cancellationToken);

            return await stream.ReadJSON<T>();
        }
    }
}