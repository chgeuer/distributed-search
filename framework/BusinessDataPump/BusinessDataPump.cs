namespace Mercury.BusinessDataPump
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
    using Mercury.Fundamentals;
    using Mercury.Interfaces;
    using Mercury.Messaging;
    using Mercury.Utils.Extensions;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;
    using static Mercury.Fundamentals.BusinessData;

    public class BusinessDataPump<TBusinessData, TBusinessDataUpdate>
    {
        private readonly Func<TBusinessData, TBusinessDataUpdate, TBusinessData> applyUpdate;
        private readonly Func<TBusinessData> createEmptyBusinessData;
        private readonly IWatermarkMessageClient<TBusinessDataUpdate> updateMessagingClient;
        private readonly BlobContainerClient snapshotContainerClient;
        private CancellationTokenSource cts;
        private Watermark lastWrittenWatermark = Watermark.NewWatermark(-1);
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
                MessagingClients.Updates<TBusinessDataUpdate>(demoCredential: demoCredential);
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

            var updateFunc = this.applyUpdate.ToFSharpFunc();

            FSharpResult<BusinessData<TBusinessData>, BusinessDataUpdateError> snapshotResult = await this.FetchBusinessDataSnapshot(this.cts.Token);
            if (snapshotResult.IsError)
            {
                return Observable.Throw<BusinessData<TBusinessData>>(((BusinessDataUpdateError.SnapshotDownloadError)snapshotResult.ErrorValue).Item);
            }

            var snapshot = snapshotResult.ResultValue;

            IConnectableObservable<BusinessData<TBusinessData>> connectableObservable =
                this.updateMessagingClient
                    .CreateWatermarkObervable(
                        startingPosition: SeekPosition.NewFromWatermark(
                            snapshot.Watermark.Add(1)),
                        cancellationToken: this.cts.Token)
                    .Scan(
                        seed: snapshotResult,
                        accumulator: (businessData, updateMessage)
                            => updateBusinessData(updateFunc, businessData, updateMessage)) // .Where(d => d.IsOk)
                    .Select(d => d.ResultValue)
                    .StartWith(snapshot)
                    .Publish(initialValue: snapshot);

            _ = connectableObservable.Connect();

            return connectableObservable.AsObservable();
        }

        public Task<Watermark> SendUpdate(TBusinessDataUpdate update, CancellationToken cancellationToken = default)
            => this.updateMessagingClient.SendWatermarkMessage(update, cancellationToken);

        /// <summary>
        /// Fetches a business data snapshot from blob storage.
        /// </summary>
        /// <param name="cancellationToken">the CancellationToken.</param>
        /// <returns>the most recent snapshot.</returns>
        public async Task<FSharpResult<BusinessData<TBusinessData>, BusinessDataUpdateError>> FetchBusinessDataSnapshot(CancellationToken cancellationToken)
        {
            try
            {
                FSharpOption<(Watermark, string)> someWatermarkAndName = await this.GetLatestSnapshotID(cancellationToken);

                if (FSharpOption<(Watermark, string)>.get_IsNone(someWatermarkAndName))
                {
                    var emptyBusinessData = new BusinessData<TBusinessData>(
                            data: this.createEmptyBusinessData(),
                            watermark: Watermark.NewWatermark(-1));

                    return FSharpResult<BusinessData<TBusinessData>, BusinessDataUpdateError>.NewOk(emptyBusinessData);
                }

                var (watermark, blobName) = someWatermarkAndName.Value;
                await Console.Out.WriteLineAsync($"Loading snapshot watermark {watermark.Item} from {blobName}");

                var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: blobName);
                var result = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                var val = await result.Value.Content.ReadJSON<BusinessData<TBusinessData>>();

                return FSharpResult<BusinessData<TBusinessData>, BusinessDataUpdateError>.NewOk(val);
            }
            catch (Exception ex)
            {
                return FSharpResult<BusinessData<TBusinessData>, BusinessDataUpdateError>.NewError(
                    BusinessDataUpdateError.NewSnapshotDownloadError(ex));
            }
        }

        public async Task<string> WriteBusinessDataSnapshot(BusinessData<TBusinessData> businessData, CancellationToken cancellationToken = default)
        {
            var blobName = WatermarkToBlobName(businessData);
            var blobClient = this.snapshotContainerClient.GetBlobClient(blobName: blobName);

            if (businessData.Watermark == this.lastWrittenWatermark)
            {
                await Console.Error.WriteLineAsync($"Skip writing {blobClient.Name} (no change)");
                return blobClient.Name;
            }

            try
            {
                _ = await blobClient.UploadAsync(content: businessData.AsJSONStream(), overwrite: false, cancellationToken: cancellationToken);
                this.lastWrittenWatermark = businessData.Watermark;
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
                        blobName: WatermarkToBlobName(snapshot.Item1),
                        snapshotsOption: DeleteSnapshotsOption.IncludeSnapshots,
                        cancellationToken: cancellationToken);
                }

                await Task.Delay(delay: sleepTime, cancellationToken: cancellationToken);
            }
        }

        public async Task<IEnumerable<(Watermark, DateTimeOffset)>> GetOldSnapshots(TimeSpan maxAge, CancellationToken cancellationToken)
        {
            var blobs = this.snapshotContainerClient.GetBlobsAsync(traits: BlobTraits.Metadata, cancellationToken: cancellationToken);
            var items = new List<(Watermark, DateTimeOffset)>();
            await foreach (var blob in blobs)
            {
                var someUpdateWatermark = ParseWatermarkFromBlobName(blob.Name);

                if (FSharpOption<Watermark>.get_IsSome(someUpdateWatermark) &&
                    blob.Properties.LastModified.HasValue &&
                    blob.Properties.LastModified.Value < DateTime.UtcNow.Subtract(maxAge))
                {
                    var v = (someUpdateWatermark.Value, blob.Properties.LastModified.Value);
                    items.Add(v);
                }
            }

            return items
                .OrderBy(i => i.Item2)
                .Where(i => i.Item1.Item % 10 == 0)
                .SkipLast(10);
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

        private async Task<FSharpOption<(Watermark, string)>> GetLatestSnapshotID(CancellationToken cancellationToken)
        {
            var names = await this.GetBlobNames(cancellationToken);
            var items = new List<(Watermark, string)>();

            foreach (var name in names)
            {
                var someUpdateWatermark = ParseWatermarkFromBlobName(name);

                if (FSharpOption<Watermark>.get_IsSome(someUpdateWatermark))
                {
                    var v = (someUpdateWatermark.Value, name);
                    items.Add(v);
                }
            }

            if (items.Count == 0)
            {
                return FSharpOption<(Watermark, string)>.None;
            }

            return FSharpOption<(Watermark, string)>.Some(items.OrderByDescending(_ => _.Item1).First());
        }

        private static FSharpOption<Watermark> ParseWatermarkFromBlobName(string n)
            => long.TryParse(n.Replace(".json", string.Empty), out long watermark)
               ? FSharpOption<Watermark>.Some(Watermark.NewWatermark(watermark))
               : FSharpOption<Watermark>.None;

        private static string WatermarkToBlobName(Watermark watermark) => $"{watermark.Item}.json";

        private static string WatermarkToBlobName(BusinessData<TBusinessData> businessData) => WatermarkToBlobName(businessData.Watermark);
    }
}