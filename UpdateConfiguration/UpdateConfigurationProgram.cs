namespace UpdateConfiguration
{
    using System;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using Fashion.BusinessData;
    using Fashion.Domain;
    using Interfaces;

    class UpdateConfigurationProgram
    {
        static async Task Main()
        {
            Console.Title = "Update Configuration";

            IDistributedSearchConfiguration demoCredential = new DemoCredential();

            var businessDataUpdates = new BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate>(
                demoCredential: demoCredential,
                createEmptyBusinessData: Code.newFashionBusinessData,
                applyUpdate: FashionBusinessDataExtensions.ApplyFashionUpdate, 
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{demoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: demoCredential.AADServicePrincipal));

            var fashionType = FashionTypes.Hat;
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

                await businessDataUpdates.SendUpdate(update);

                await Console.Out.WriteLineAsync($"Update sent for {newMarkup}");
            }
        }
    }
}