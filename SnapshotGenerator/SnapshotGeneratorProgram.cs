namespace SnapshotGenerator
{
    using Azure.Messaging.EventHubs.Consumer;
    using Credentials;
    using System;
    using System.Threading.Tasks;

    class SnapshotGeneratorProgram
    {
        static async Task Main()
        {
            Console.Title = "Snapshot Generator";

            await using var eventHubConsumerClient = new EventHubConsumerClient(
                consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: DemoCredential.EventHubTopicNameBusinessDataUpdates,
                credential: DemoCredential.AADServicePrincipal);


            await Console.Out.WriteLineAsync("Hello World!");
        }
    }
}
