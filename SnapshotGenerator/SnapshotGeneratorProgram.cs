namespace SnapshotGenerator
{
    using System;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using Interfaces;

    /// <summary>
    /// This utility can run on multiple instances. In the worst case, we generate snapshots multiple times.
    /// </summary>
    class SnapshotGeneratorProgram
    {
        static async Task Main()
        {
            Console.Title = "Snapshot Generator";

            using var businessDataUpdates = new BusinessDataProvider(
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: DemoCredential.AADServicePrincipal),
                eventHubConsumerClient: new EventHubConsumerClient(
                    consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                    fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                    eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                    credential: DemoCredential.AADServicePrincipal));

            await businessDataUpdates.StartUpdateLoop();

            var bd = businessDataUpdates.GetBusinessData();
            var (lastOffsetWritten, lastUpdateWritten) = (bd.Version, DateTime.MinValue);
            while (true)
            {
                var oldBd = bd;
                bd = businessDataUpdates.GetBusinessData();

                if (oldBd.Version != bd.Version)
                {
                    await Console.Out.WriteLineAsync($"{bd.AsJSON()}");
                }

                var snapshotMaxAge = TimeSpan.FromSeconds(10);
                if (bd.Version != lastOffsetWritten && DateTime.UtcNow.Subtract(lastUpdateWritten) > snapshotMaxAge)
                {
                    var snapshotName = await businessDataUpdates.WriteBusinessDataSnapshot(bd);
                    (lastOffsetWritten, lastUpdateWritten) = (bd.Version, DateTime.UtcNow);
                    await Console.Out.WriteLineAsync($"wrote snapshot {snapshotName}");
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}