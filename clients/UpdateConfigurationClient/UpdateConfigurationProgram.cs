namespace Mercury.Clients.UpdateConfiguration
{
    using Azure.Storage.Blobs;
    using Mercury.BusinessDataPump;
    using Mercury.Credentials;
    using Mercury.Customer.Fashion;
    using Mercury.Interfaces;
    using System;
    using System.Threading.Tasks;
    using static Mercury.Customer.Fashion.BusinessData;

    // A simple client, which sends update commands *directly* into Kafka. 
    // In reality, this needs to send HTTP requests into the "Business Data Service"
    internal class UpdateConfigurationProgram
    {
        private static async Task Main()
        {
            Console.Title = "Update Configuration";

            IDistributedSearchConfiguration demoCredential = new DemoCredential();

            var businessDataUpdates = new BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate>(
                demoCredential: demoCredential,
                createEmptyBusinessData: newFashionBusinessData,
                applyUpdate: FashionBusinessDataExtensions.ApplyFashionUpdate,
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{demoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: demoCredential.AADServicePrincipal));

            while (true)
            {
                await Console.Out.WriteAsync($"Please enter an item and a price, separated by a space: ");
                var input = await Console.In.ReadLineAsync();
                var values = input.Split(" ");
                if (values.Length != 2)
                {
                    continue;
                }
                var item = values[0];

                if (!decimal.TryParse(values[1], out var newMarkup))
                {
                    continue;
                }

                var update = FashionBusinessDataUpdate.NewMarkupUpdate(
                        fashionType: item,
                        markupPrice: newMarkup);

                // Here, we're directly dropping the update in Kafka.
                // This must be replaced with a secured HTTP request.
                await businessDataUpdates.SendUpdate(update);

                await Console.Out.WriteLineAsync($"Update sent for {newMarkup}");
            }
        }
    }
}