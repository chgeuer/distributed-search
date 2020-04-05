namespace BusinessDataAggregation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using DataTypesFSharp;
    using Interfaces;
    using LanguageExt;
    using Messaging.AzureImpl;
    using Messaging.KafkaImpl;
    using Microsoft.FSharp.Collections;
    using static Fundamentals.Types;

    public class BusinessDataPump
    {
        private static bool ParseOffsetFromBlobName(string n, out long offset) => long.TryParse(n.Replace(".json", string.Empty), out offset);

        private static string OffsetToBlobName(long offset) => $"{offset}.json";

        private readonly BlobContainerClient snapshotContainerClient;
        private readonly IMessageClient<BusinessDataUpdate> updateMessagingClient;
        private CancellationTokenSource cts;
        private long lastWrittenOffset = -1;
        private Task deletetionTask;

        public BusinessDataPump(BlobContainerClient snapshotContainerClient)
        {
            this.updateMessagingClient = AzureMessagingClients.Updates<BusinessDataUpdate>(partitionId: "0");
            this.snapshotContainerClient = snapshotContainerClient;
        }

        public BusinessData BusinessData { get; private set; }

        public async Task StartUpdateProcess(CancellationToken cancellationToken = default)
        {
            IObservable<BusinessData> businessDataObservable = await this.CreateObservable(cancellationToken);
            businessDataObservable.Subscribe(
                onNext: bd =>
                {
                    this.BusinessData = bd;
                },
                cancellationToken);
        }

        public async Task<IObservable<BusinessData>> CreateObservable(CancellationToken cancellationToken = default)
        {
            this.deletetionTask = Task.Run(() => this.DeleteOldSnapshots(
                maxAge: TimeSpan.FromDays(1),
                sleepTime: TimeSpan.FromMinutes(1),
                cancellationToken: this.cts.Token));

            this.cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            BusinessData snapshotValue = await this.FetchBusinessDataSnapshot(this.cts.Token);

            IConnectableObservable<BusinessData> connectableObservable =
                this.updateMessagingClient
                    .CreateObervable(
                        startingPosition: SeekPosition.NewFromOffset(
                            updateOffset: snapshotValue.Version),
                        cancellationToken: this.cts.Token)
                    .Scan(
                        seed: snapshotValue,
                        accumulator: (businessData, msg) => businessData.ApplyUpdates(new[] { Tuple.Create(msg.Offset, msg.Payload) }))
                    .StartWith(snapshotValue)
                    .Publish(initialValue: snapshotValue);

            _ = connectableObservable.Connect();

            return connectableObservable
                .AsObservable();
        }

        public Task SendUpdate(BusinessDataUpdate update, CancellationToken cancellationToken = default)
            => this.updateMessagingClient.SendMessage(update, cancellationToken);

        public async Task<BusinessData> FetchBusinessDataSnapshot(CancellationToken cancellationToken)
        {
            var someOffsetAndName = await this.GetLatestSnapshotID(cancellationToken);
            return await someOffsetAndName.Match(
                Some: async offsetAndName =>
                {
                    await Console.Out.WriteLineAsync($"Loading snapshot offset {offsetAndName.Item1} from {offsetAndName.Item2}");

                    var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: offsetAndName.Item2);
                    var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                    return await result.Value.Content.ReadJSON<BusinessData>();
                },
                None: () => Task.FromResult(this.EmptyBusinessData()));
        }

        public async Task<string> WriteBusinessDataSnapshot(BusinessData businessData, CancellationToken cancellationToken = default)
        {
            var blobName = OffsetToBlobName(businessData.Version);
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

        public async Task DeleteOldSnapshots(TimeSpan maxAge, TimeSpan sleepTime, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshots = await this.GetOldSnapshots(maxAge, cancellationToken);
                foreach (var snapshot in snapshots)
                {
                    await Console.Out.WriteLineAsync($"Delete snapshot {snapshot.Item1} from {snapshot.Item2}");
                    await this.snapshotContainerClient.DeleteBlobIfExistsAsync(
                        blobName: OffsetToBlobName(snapshot.Item1),
                        snapshotsOption: DeleteSnapshotsOption.IncludeSnapshots,
                        cancellationToken: cancellationToken);
                }

                await Task.Delay(delay: sleepTime, cancellationToken: cancellationToken);
            }
        }

        public async Task<IEnumerable<(long, DateTimeOffset)>> GetOldSnapshots(TimeSpan maxAge, CancellationToken cancellationToken)
        {
            var blobs = this.snapshotContainerClient.GetBlobsAsync(traits: BlobTraits.Metadata, cancellationToken: cancellationToken);
            var items = new List<(long, DateTimeOffset)>();
            await foreach (var blob in blobs)
            {
                if (ParseOffsetFromBlobName(blob.Name, out var offset) &&
                    blob.Properties.LastModified.HasValue &&
                    blob.Properties.LastModified.Value < DateTime.UtcNow.Subtract(maxAge))
                {
                    var v = (offset, blob.Properties.LastModified.Value);
                    items.Add(v);
                }
            }

            return items
                .OrderBy(i => i.Item2)
                .SkipLast(5);
        }

        private async Task<Option<(long, string)>> GetLatestSnapshotID(CancellationToken cancellationToken)
        {
            var blobs = this.snapshotContainerClient.GetBlobsAsync(cancellationToken: cancellationToken);
            var items = new List<(long, string)>();
            await foreach (var blob in blobs)
            {
                if (ParseOffsetFromBlobName(blob.Name, out var offset))
                {
                    var v = (offset, blob.Name);
                    items.Add(v);
                }
            }

            if (items.Count == 0)
            {
                return Option<(long, string)>.None;
            }

            return Option<(long, string)>.Some(items.OrderByDescending(_ => _.Item1).First());
        }

        private BusinessData EmptyBusinessData() => new BusinessData(
            markup: new FSharpMap<string, decimal>(Array.Empty<Tuple<string, decimal>>()),
            brands: new FSharpMap<string, string>(Array.Empty<Tuple<string, string>>()),
            defaultMarkup: 0m,
            version: -1);
    }
}