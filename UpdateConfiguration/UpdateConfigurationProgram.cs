namespace UpdateConfiguration
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Producer;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;

    class UpdateConfigurationProgram
    {
        static async Task Main()
        {
            Console.Title = "Update Configuration";

            await using var eventHubProducerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                credential: DemoCredential.AADServicePrincipal);

            var cts = new CancellationTokenSource();

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

                var eventData = new EventData(eventBody: update.AsJSON().ToUTF8Bytes());

                using EventDataBatch batchOfOne = await eventHubProducerClient.CreateBatchAsync();
                batchOfOne.TryAdd(eventData);
                await eventHubProducerClient.SendAsync(batchOfOne);

                await Console.Out.WriteLineAsync($"Update sent for {newMarkup}");
            }
        }
    }
}