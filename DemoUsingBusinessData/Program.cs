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
    using Fashion.BusinessData.Logic;

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Business Data User";
            var cts = new CancellationTokenSource();

            var businessDataPump = new BusinessDataPump<BusinessData, BusinessDataUpdate>(
                applyUpdate: (bd, updateM) => bd.ApplyUpdates(new[] { Tuple.Create(updateM.Offset, updateM.Payload) }),
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: DemoCredential.AADServicePrincipal));

            Func<BusinessData, string> bdToStr = bd => string.Join(" ",
                        bd.Markup.Select( kvp => $"{kvp.Key}={kvp.Value}").ToArray());

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