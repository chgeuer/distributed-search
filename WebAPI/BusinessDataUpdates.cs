namespace WebAPI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;

    public static class BusinessDataUpdates
    {
        private static readonly Task UpdateTask;

        private static Lazy<BusinessData> businessData =
            new Lazy<BusinessData>(() => FetchBusinessDataSnapshot().Result);

        static BusinessDataUpdates()
        {
            // This fire-and-forget task simulates the price for Hats going up by a cent each second. Inflationary 🤣
            // In reality, this is the update feed for business and decision data
            BusinessDataUpdates.UpdateTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    var fashionType = FashionTypes.Hat;
                    var newMarkup = businessData.Value.Markup[fashionType] + 0_01m;
                    var newVersion = businessData.Value.Version + 1;

                    businessData = new Lazy<BusinessData>(businessData.Value.AddMarkup(newVersion, fashionType, newMarkup));
                    await Console.Out.WriteLineAsync($"Updated markup for {fashionType} to version v{newVersion}: EUR {newMarkup / 100}");
                }
            });
        }

        public static Func<BusinessData> GetBusinessData()
        {
            return () =>
            {
                var b = businessData.Value;
                Console.Out.WriteLine($"Somebody wants business data, so we hand out version {b.Version}");
                return b;
            };
        }

        private static async Task<BusinessData> FetchBusinessDataSnapshot()
        {
            var snapshotContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                credential: DemoCredential.AADServicePrincipal);

            Func<string, long> blobNameToOffset = n => long.TryParse(n.Replace(".json", string.Empty), out var l) ? l : -1;
            Func<long, string> offsetToBlobName = o => $"{o}.json";

            var blobs = snapshotContainerClient.GetBlobsAsync();
            var items = new List<long>();
            await foreach (var blob in blobs)
            {
                items.Add(blobNameToOffset(blob.Name));
            }

            if (items.Count == 0)
            {
                return null;
            }

            var offset = items.Max();
            if (offset == -1)
            {
                return null;
            }

            var blobClient = snapshotContainerClient.GetBlobClient(blobName: offsetToBlobName(offset));
            var result = await blobClient.DownloadAsync();
            return await result.Value.Content.ReadJSON<BusinessData>();
        }
    }
}