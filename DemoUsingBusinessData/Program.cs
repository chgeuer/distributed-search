namespace DemoUsingBusinessData
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;

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

            //await businessDataPump.StartUpdateProcess(cts.Token);
            //while (true)
            //{
            //    var bd = businessDataPump.BusinessData;
            //    await Console.Out.WriteLineAsync(string.Join(" ", 
            //        bd.Markup.Select(
            //            kvp => $"{kvp.Key}={kvp.Value}").ToArray()));
            //}

            var businessDataObservable = await businessDataPump.CreateObservable(cts.Token);
            businessDataObservable.Subscribe(onNext: async bd =>
                {
                    await Console.Out.WriteLineAsync(string.Join(" ",
                        bd.Markup.Select(
                            kvp => $"{kvp.Key}={kvp.Value}").ToArray()));
                },
                token: cts.Token);

            Console.ReadLine();
            cts.Cancel();
        }
    }
}