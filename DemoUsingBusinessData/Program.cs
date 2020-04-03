namespace DemoUsingBusinessData
{
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Business Data User";
            var cts = new CancellationTokenSource();

            var businessDataPump = new BusinessDataPump(
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: DemoCredential.AADServicePrincipal));

            await businessDataPump.StartUpdateProcess(cts.Token);


            while (true)
            {
                await Console.Out.WriteLineAsync(string.Join(" ", 
                    businessDataPump.BusinessData.Markup.Select(
                        kvp => $"{kvp.Key}={kvp.Value}").ToArray()));
            }
        }
    }
}
