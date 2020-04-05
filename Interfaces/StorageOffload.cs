namespace Interfaces
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public class StorageOffload
    {
        private readonly Func<string, Stream, CancellationToken, Task> upload;
        private readonly Func<string, CancellationToken, Task<Stream>> download;

        public StorageOffload(
            Func<string, Stream, CancellationToken, Task> upload,
            Func<string, CancellationToken, Task<Stream>> download)
        {
            (this.upload, this.download) = (upload, download);
        }

        public Task Upload(string blobName, Stream stream, CancellationToken cancellationToken)
        {
            return this.upload(blobName, stream, cancellationToken);
        }

        public async Task<T> Download<T>(string blobName, CancellationToken cancellationToken)
        {
            var stream = await this.download(blobName, cancellationToken);
            return await stream.ReadJSON<T>();
        }
    }
}
