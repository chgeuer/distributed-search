namespace SnapshotGeneratorService
{
    using Azure.Storage.Blobs;
    using Mercury.BusinessDataPump;
    using Mercury.Credentials;
    using Mercury.Customer.Fashion;
    using Mercury.Interfaces;
    using Mercury.Utils.Extensions;
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using static Mercury.Customer.Fashion.BusinessData;
    using static Mercury.Fundamentals.Types;

    /// <summary>
    /// This utility can run on multiple instances. In the worst case, we generate snapshots multiple times.
    /// </summary>
    internal class SnapshotGeneratorProgram
    {
        private static async Task Main()
        {
            Console.Title = "Snapshot Generator Service";

            IDistributedSearchConfiguration demoCredential = new DemoCredential();

            var businessDataPump = new BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate>(
                demoCredential: demoCredential,
                createEmptyBusinessData: newFashionBusinessData,
                applyUpdate: FashionExtensions.ApplyFashionUpdate,
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{demoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: demoCredential.AADServicePrincipal));

            var cts = new CancellationTokenSource();
            IObservable<BusinessData<FashionBusinessData>> businessDataObservable = await businessDataPump.CreateObservable(cts.Token);

            businessDataObservable
                .Sample(interval: TimeSpan.FromSeconds(5))
                .Subscribe(
                    onNext: async bd =>
                    {
                        await Console.Out.WriteLineAsync($"{bd.AsJSON()}");
                        var snapshotName = await businessDataPump.WriteBusinessDataSnapshot(bd);
                        await Console.Out.WriteLineAsync($"wrote snapshot {snapshotName}");
                    },
                    onError: ex => Console.Error.WriteLine($"ERROR: {ex.Message}"),
                    onCompleted: () => Console.Out.WriteLine($"Completed"),
                    token: cts.Token);

            await Console.Out.WriteAsync("Press <return> to stop snapshot generation");
            _ = await Console.In.ReadLineAsync();
            cts.Cancel();
        }
    }
}