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
        // This fire-and-forget task simulates the price for Hats going up by a cent each second. Inflationary 🤣
        // In reality, this is the update feed for business and decision data
        private static readonly Task UpdateTask = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    var bd = businessData.Value;
                    var v = bd.Version;

                    var fashionType = FashionTypes.Hat;
                    var newMarkup = bd.Markup[fashionType] + 0_01m;

                    var u1 = BusinessDataUpdate.NewMarkupUpdate(
                            fashionType: fashionType,
                            markupPrice: newMarkup);

                    var u2 = BusinessDataUpdate.NewBrandUpdate(
                        brandAcronym: Guid.NewGuid().ToString(),
                        name: "some new brand");

                    // Just derialize/deserialize for fun
                    static Task<BusinessDataUpdate> RoundTrip(BusinessDataUpdate u) => u.AsJSONStream().ReadJSON<BusinessDataUpdate>();

                    // Apply all updates
                    var newData = bd
                        .Update(
                            version: v + 1,
                            update: await RoundTrip(u1))
                        .Update(
                            version: v + 2,
                            update: await RoundTrip(u2));

                    businessData = new Lazy<BusinessData>(newData);

                    await Console.Out.WriteLineAsync($"Updated markup for {fashionType} to version v{newData.Version}: EUR {newData.Markup[fashionType] / 100}");
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Fuck: {ex.Message}");
                }
            }
        });

        private static Lazy<BusinessData> businessData =
            new Lazy<BusinessData>(() => FetchBusinessDataSnapshot().Result);

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

            static long BlobNameToOffset(string n) => long.TryParse(n.Replace(".json", string.Empty), out var l) ? l : -1;
            static string OffsetToBlobName(long o) => $"{o}.json";

            var blobs = snapshotContainerClient.GetBlobsAsync();
            var items = new List<long>();
            await foreach (var blob in blobs)
            {
                items.Add(BlobNameToOffset(blob.Name));
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

            var blobClient = snapshotContainerClient.GetBlobClient(blobName: OffsetToBlobName(offset));
            var result = await blobClient.DownloadAsync();
            return await result.Value.Content.ReadJSON<BusinessData>();
        }
    }
}