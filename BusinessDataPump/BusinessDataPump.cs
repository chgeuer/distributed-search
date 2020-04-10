namespace BusinessDataPump
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
    using Interfaces;
    using Messaging;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;

    public class BusinessDataPump<TBusinessData, TBusinessDataUpdate>
    {
        private static FSharpOption<Offset> ParseOffsetFromBlobName(string n)
             => long.TryParse(n.Replace(".json", string.Empty), out long offset)
                ? FSharpOption<Offset>.Some(Offset.NewOffset(offset))
                : FSharpOption<Offset>.None;

        private static string OffsetToBlobName(Offset offset) => $"{offset.Item}.json";

        private static string OffsetToBlobName(BusinessData<TBusinessData> businessData) => OffsetToBlobName(businessData.Offset);

        private readonly Func<TBusinessData, TBusinessDataUpdate, TBusinessData> applyUpdate;
        private readonly Func<TBusinessData> createEmptyBusinessData;
        private readonly IMessageClient<TBusinessDataUpdate> updateMessagingClient;
        private readonly BlobContainerClient snapshotContainerClient;
        private CancellationTokenSource cts;
        private Offset lastWrittenOffset = Offset.NewOffset(-1);
        private Task deletetionTask;

        public BusinessDataPump(
            IDistributedSearchConfiguration demoCredential,
            Func<TBusinessData> createEmptyBusinessData,
            Func<TBusinessData, TBusinessDataUpdate, TBusinessData> applyUpdate,
            BlobContainerClient snapshotContainerClient)
        {
            this.applyUpdate = applyUpdate;
            this.createEmptyBusinessData = createEmptyBusinessData;
            this.updateMessagingClient =
                MessagingClients.Updates<TBusinessDataUpdate>(
                    demoCredential: demoCredential,
                    partitionId: 0); // TODO
            this.snapshotContainerClient = snapshotContainerClient;
        }

        public BusinessData<TBusinessData> BusinessData { get; private set; }

        public async Task StartUpdateProcess(CancellationToken cancellationToken = default)
        {
            IObservable<BusinessData<TBusinessData>> businessDataObservable = await this.CreateObservable(cancellationToken);
            businessDataObservable.Subscribe(
                onNext: bd => this.BusinessData = bd,
                cancellationToken);
        }

        public async Task<IObservable<BusinessData<TBusinessData>>> CreateObservable(CancellationToken cancellationToken = default)
        {
            this.cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            this.deletetionTask = Task.Run(() => this.DeleteOldSnapshots(
                maxAge: TimeSpan.FromDays(1),
                sleepTime: TimeSpan.FromMinutes(1),
                cancellationToken: this.cts.Token));

            BusinessData<TBusinessData> snapshotValue = await this.FetchBusinessDataSnapshot(this.cts.Token);

            // TODO Use curried function / partial application here?
            var updateFunc = this.applyUpdate.ToFSharpFunc();

            IConnectableObservable<BusinessData<TBusinessData>> connectableObservable =
                this.updateMessagingClient
                    .CreateObervable(
                        startingPosition: SeekPosition.NewFromOffset(snapshotValue.Offset),
                        cancellationToken: this.cts.Token)
                    .Scan(
                        seed: snapshotValue,
                        accumulator: (businessData, updateMessage) => updateBusinessData(updateFunc, businessData, updateMessage))
                    .StartWith(snapshotValue)
                    .Publish(initialValue: snapshotValue);

            _ = connectableObservable.Connect();

            return connectableObservable.AsObservable();
        }

        public Task SendUpdate(TBusinessDataUpdate update, CancellationToken cancellationToken = default)
            => this.updateMessagingClient.SendMessage(update, cancellationToken);

        public async Task<BusinessData<TBusinessData>> FetchBusinessDataSnapshot(CancellationToken cancellationToken)
        {
            FSharpOption<(Offset, string)> someOffsetAndName = await this.GetLatestSnapshotID(cancellationToken);

            if (FSharpOption<(Offset, string)>.get_IsNone(someOffsetAndName))
            {
                return new BusinessData<TBusinessData>(
                    data: this.createEmptyBusinessData(),
                    offset: Offset.NewOffset(-1));
            }

            var (offset, blobName) = someOffsetAndName.Value;
            await Console.Out.WriteLineAsync($"Loading snapshot offset {offset.Item} from {blobName}");

            var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: blobName);
            var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
            return await result.Value.Content.ReadJSON<BusinessData<TBusinessData>>();
        }

        public async Task<string> WriteBusinessDataSnapshot(BusinessData<TBusinessData> businessData, CancellationToken cancellationToken = default)
        {
            var blobName = OffsetToBlobName(businessData);
            var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: blobName);

            if (businessData.Offset == this.lastWrittenOffset)
            {
                await Console.Error.WriteLineAsync($"Skip writing {blobClient.Name} (no change)");
                return blobClient.Name;
            }

            try
            {
                _ = await blobClient.UploadAsync(content: businessData.AsJSONStream(), overwrite: false, cancellationToken: cancellationToken);
                this.lastWrittenOffset = businessData.Offset;
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

        public async Task<IEnumerable<(Offset, DateTimeOffset)>> GetOldSnapshots(TimeSpan maxAge, CancellationToken cancellationToken)
        {
            var blobs = this.snapshotContainerClient.GetBlobsAsync(traits: BlobTraits.Metadata, cancellationToken: cancellationToken);
            var items = new List<(Offset, DateTimeOffset)>();
            await foreach (var blob in blobs)
            {
                var someUpdateOffset = ParseOffsetFromBlobName(blob.Name);

                if (FSharpOption<Offset>.get_IsSome(someUpdateOffset) &&
                    blob.Properties.LastModified.HasValue &&
                    blob.Properties.LastModified.Value < DateTime.UtcNow.Subtract(maxAge))
                {
                    var v = (someUpdateOffset.Value, blob.Properties.LastModified.Value);
                    items.Add(v);
                }
            }

            return items
                .OrderBy(i => i.Item2)
                .SkipLast(5);
        }

        private async Task<IEnumerable<string>> GetBlobNames(CancellationToken cancellationToken)
        {
            var blobs = this.snapshotContainerClient.GetBlobsAsync(cancellationToken: cancellationToken);
            var items = new List<string>();
            await foreach (var blob in blobs)
            {
                items.Add(blob.Name);
            }

            return items;
        }

        private async Task<FSharpOption<(Offset, string)>> GetLatestSnapshotID(CancellationToken cancellationToken)
        {
            var names = await this.GetBlobNames(cancellationToken);
            var items = new List<(Offset, string)>();

            foreach (var name in names)
            {
                var someUpdateOffset = ParseOffsetFromBlobName(name);

                if (FSharpOption<Offset>.get_IsSome(someUpdateOffset))
                {
                    var v = (someUpdateOffset.Value, name);
                    items.Add(v);
                }
            }

            if (items.Count == 0)
            {
                return FSharpOption<(Offset, string)>.None;
            }

            return FSharpOption<(Offset, string)>.Some(items.OrderByDescending(_ => _.Item1).First());
        }
    }
}