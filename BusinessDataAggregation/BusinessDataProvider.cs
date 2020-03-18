namespace BusinessDataAggregation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Storage.Blobs;
    using DataTypesFSharp;
    using Interfaces;
    using Microsoft.FSharp.Collections;

    public class BusinessDataProvider : IAsyncDisposable, IDisposable
    {
        // This fire-and-forget task simulates the price for Hats going up by a cent each second. Inflationary 🤣
        // In reality, this is the update feed for business and decision data
        private readonly BlobContainerClient snapshotContainerClient;
        private readonly EventHubConsumerClient eventHubConsumerClient;
        private bool running = false;
        private CancellationTokenSource cts;
        private BusinessData businessData;
        private long lastWrittenOffset = -1;
        private bool disposed = false;

        public BusinessDataProvider(BlobContainerClient snapshotContainerClient, EventHubConsumerClient eventHubConsumerClient)
        {
            this.snapshotContainerClient = snapshotContainerClient;
            this.eventHubConsumerClient = eventHubConsumerClient;
            this.businessData = this.EmptyBusinessData();
        }

        public async Task StartUpdateLoop(CancellationToken cancellationToken = default)
        {
            if (this.running)
            {
                return;
            }

            this.running = true;

            this.cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            this.businessData = await this.FetchBusinessDataSnapshot(this.cts.Token);

            this.eventHubConsumerClient
                .CreateObservable(
                    partitionId: "0",
                    startingPosition: EventPosition.FromOffset(
                        offset: this.businessData.Version,
                        isInclusive: true),
                    cancellationToken: cancellationToken)
                .Select(partitionEvent => partitionEvent.Data)
                .Select(eventData => new { eventData.Offset, JsonString = eventData.GetBodyAsUTF8() })
                .Select(x => new { x.Offset, BusinessDataUpdate = x.JsonString.DeserializeJSON<BusinessDataUpdate>() })
                .Subscribe(
                    onNext: o =>
                    {
                        // here we're only applying a single update
                        var updates = new (long, BusinessDataUpdate)[] { (o.Offset, o.BusinessDataUpdate), }
                            .Select(TupleExtensions.ToTuple)
                            .ToFSharp();

                        // side effect
                        this.businessData = this.businessData.ApplyUpdates(updates);
                    },
                    onError: ex => { },
                    onCompleted: () => { },
                    token: cancellationToken);

            await Console.Out.WriteLineAsync("Launched observer");
        }

        public BusinessData GetBusinessData()
        {
            if (!this.running)
            {
                throw new NotSupportedException($"{typeof(BusinessDataProvider).Name} instance is not yet started. Call {nameof(BusinessDataProvider.StartUpdateLoop)} first.");
            }

            return this.businessData;
        }

        public async Task<BusinessData> FetchBusinessDataSnapshot(CancellationToken cancellationToken)
        {
            static (bool, long) BlobNameToOffset(string n) => long.TryParse(n.Replace(".json", string.Empty), out var l) ? (true, l) : (false, -1);

            var (offset, name) = await this.GetLatestSnapshotID(BlobNameToOffset, cancellationToken);
            if (offset == -1)
            {
                return this.EmptyBusinessData();
            }

            await Console.Out.WriteLineAsync($"Loading snapshot offset {offset} from {name}");

            var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: name);
            var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
            return await result.Value.Content.ReadJSON<BusinessData>();
        }

        public async Task<string> WriteBusinessDataSnapshot(BusinessData businessData, CancellationToken cancellationToken = default)
        {
            // var businessData = this.GetBusinessData();
            var blobName = $"{businessData.Version}.json";
            var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: blobName);

            if (businessData.Version == this.lastWrittenOffset)
            {
                await Console.Error.WriteLineAsync($"Skip writing {blobClient.Name} (no change)");
                return blobClient.Name;
            }

            try
            {
                _ = await blobClient.UploadAsync(content: businessData.AsJSONStream(), overwrite: false, cancellationToken: cancellationToken);
                this.lastWrittenOffset = businessData.Version;
            }
            catch (RequestFailedException rfe) when (rfe.ErrorCode == "BlobAlreadyExists")
            {
            }

            return blobClient.Name;
        }

        public virtual ValueTask DisposeAsync()
        {
            try
            {
                this.Dispose();
                return default;
            }
            catch (Exception exception)
            {
                return new ValueTask(Task.FromException(exception));
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.cts != null)
                {
                    this.cts.Cancel();
                }
            }

            this.disposed = true;
        }

        private async Task<(long, string)> GetLatestSnapshotID(Func<string, (bool, long)> blobNameToOffset, CancellationToken cancellationToken)
        {
            var blobs = this.snapshotContainerClient.GetBlobsAsync(cancellationToken: cancellationToken);
            var items = new List<(long, string)>();
            await foreach (var blob in blobs)
            {
                var (validated, offset) = blobNameToOffset(blob.Name);
                if (validated)
                {
                    var v = (offset, blob.Name);
                    items.Add(v);
                }
            }

            if (items.Count == 0)
            {
                return (-1, string.Empty);
            }

            return items.OrderByDescending(_ => _.Item1).First();
        }

        private BusinessData EmptyBusinessData() => new BusinessData(
            markup: new FSharpMap<string, decimal>(new Tuple<string, decimal>[0]),
            brands: new FSharpMap<string, string>(new Tuple<string, string>[0]),
            defaultMarkup: 0m,
            version: -1);
    }
}