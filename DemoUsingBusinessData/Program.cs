namespace DemoUsingBusinessData
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using Fashion.BusinessData;
    using Interfaces;
    using static Fundamentals.Types;

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Business Data User";

            IDistributedSearchConfiguration demoCredential = new DemoCredential();

            var cts = new CancellationTokenSource();

            var businessDataPump = new BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate>(
                demoCredential: demoCredential,
                createEmptyBusinessData: Code.newFashionBusinessData,
                applyUpdate: FashionBusinessDataExtensions.ApplyFashionUpdate,
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{demoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: demoCredential.AADServicePrincipal));

            Func<BusinessData<FashionBusinessData>, string> bdToStr = bd => string.Join(" ",
                        bd.Data.Markup.Select( kvp => $"{kvp.Key}={kvp.Value}").ToArray());

            bool demoObservable = true;
            if (demoObservable)
            {
                // This code observes business data changes and only prints when there have been changes
                var businessDataObservable = await businessDataPump.CreateObservable(cts.Token);
                businessDataObservable.Subscribe(onNext: async bd =>
                {
                    await Console.Out.WriteLineAsync(bdToStr(bd));
                }, token: cts.Token);

                _ = await Console.In.ReadLineAsync();
                cts.Cancel();
            }
            else
            {
                // This code runs in an endless loop, and prints the business data (regardless whether it changed or not)
                await businessDataPump.StartUpdateProcess(cts.Token);
                while (true)
                {
                    await Console.Out.WriteLineAsync(bdToStr(businessDataPump.BusinessData));
                }
            }
        }
    }
}