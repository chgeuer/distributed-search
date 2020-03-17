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

            var newMarkup = 0_01m;

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                var update = BusinessDataUpdate.NewMarkupUpdate(
                        fashionType: FashionTypes.Hat,
                        markupPrice: newMarkup);

                var eventData = new EventData(eventBody: update.AsJSON().ToUTF8Bytes());

                using EventDataBatch batchOfOne = await eventHubProducerClient.CreateBatchAsync();
                batchOfOne.TryAdd(eventData);
                await eventHubProducerClient.SendAsync(batchOfOne);

                await Console.Out.WriteLineAsync($"Update sent for {newMarkup}");

                newMarkup += 0_01m;
            }
        }
    }
}