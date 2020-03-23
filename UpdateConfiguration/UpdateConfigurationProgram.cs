namespace UpdateConfiguration
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Producer;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using DataTypesFSharp;

    class UpdateConfigurationProgram
    {
        static async Task Main()
        {
            Console.Title = "Update Configuration";

            var cts = new CancellationTokenSource();

            var businessDataUpdates = new BusinessDataProvider(
               snapshotContainerClient: new BlobContainerClient(
                   blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                   credential: DemoCredential.AADServicePrincipal),
               eventHubConsumerClient: new EventHubConsumerClient(
                   consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                   fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                   eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                   credential: DemoCredential.AADServicePrincipal),
               eventHubProducerClient: new EventHubProducerClient(
                   fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                   eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                   credential: DemoCredential.AADServicePrincipal));

            var observable = await businessDataUpdates.CreateObservable(cts.Token);

            var fashionType = FashionTypes.Hat;
            while (true)
            {
                await Console.Out.WriteAsync($"How much does a {fashionType} cost: ");
                var input = await Console.In.ReadLineAsync();
                if (!decimal.TryParse(input, out var newMarkup))
                {
                    continue;
                }

                var update = BusinessDataUpdate.NewMarkupUpdate(
                        fashionType: FashionTypes.Hat,
                        markupPrice: newMarkup);

                await businessDataUpdates.SendUpdate(update);

                await Console.Out.WriteLineAsync($"Update sent for {newMarkup}");
            }
        }
    }
}