﻿namespace SnapshotGenerator
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using Fashion;
    using Interfaces;
    using static Fashion.BusinessData;
    using static Fundamentals.Types;

    /// <summary>
    /// This utility can run on multiple instances. In the worst case, we generate snapshots multiple times.
    /// </summary>
    class SnapshotGeneratorProgram
    {
        static async Task Main()
        {
            Console.Title = "Snapshot Generator";

            IDistributedSearchConfiguration demoCredential = new DemoCredential();

            var businessDataPump = new BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate>(
                demoCredential: demoCredential,
                createEmptyBusinessData: newFashionBusinessData,
                applyUpdate: FashionBusinessDataExtensions.ApplyFashionUpdate, 
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