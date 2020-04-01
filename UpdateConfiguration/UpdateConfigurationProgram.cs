namespace UpdateConfiguration
{
    using System;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using DataTypesFSharp;

    class UpdateConfigurationProgram
    {
        static async Task Main()
        {
            Console.Title = "Update Configuration";

            var businessDataUpdates = new BusinessDataProvider(
               snapshotContainerClient: new BlobContainerClient(
                   blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                   credential: DemoCredential.AADServicePrincipal));

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