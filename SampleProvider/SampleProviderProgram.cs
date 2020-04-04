namespace SampleProvider
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Messaging.AzureImpl;
    using Microsoft.FSharp.Collections;
    using static Fundamentals.Types;

    internal class SampleProviderProgram
    {
        private static async Task Main()
        {
            Console.Title = "Sample Provider";

            var requestsClient = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>(partitionId: null);

            AzureMessagingClientWithStorageOffload<ProviderSearchResponse<FashionItem>> responseTopic(string topicName) =>
                MessagingClients.WithStorageOffload<ProviderSearchResponse<FashionItem>>(
                    topicName: topicName, partitionId: "0", accountName: DemoCredential.StorageOffloadAccountName,
                    containerName: DemoCredential.StorageOffloadContainerNameResponses);

            var cts = new CancellationTokenSource();

            static string getBlobName(ProviderSearchRequest<FashionSearchRequest> search, string requestId) => $"{requestId}/{Guid.NewGuid()}.json";

            requestsClient
                .CreateObervable(SeekPosition.FromTail, cts.Token)
                .Subscribe(
                    onNext: async searchRequestMessage =>
                    {
                        var search = searchRequestMessage.Value;
                        var requestId = searchRequestMessage.Properties.GetRequestID();

                        var responseProducer = responseTopic(search.ResponseTopic);
                        Console.Out.WriteLine($"{requestId}: Somebody's looking for {search.SearchRequest.FashionType}");

                        var tcs = new TaskCompletionSource<bool>();

                        GetResponses()
                            .Select(foundFashionItem => new ProviderSearchResponse<FashionItem>(
                                requestID: requestId,
                                response: ListModule.OfArray(new[] { foundFashionItem })))
                            .Subscribe(
                                onNext: async (responsePayload) =>
                                {
                                    var blobName = getBlobName(search, requestId);

                                    await responseProducer.Send(
                                        payload: responsePayload,
                                        requestId: requestId,
                                        blobName: blobName,
                                        cancellationToken: cts.Token);

                                    await Console.Out.WriteLineAsync($"{requestId}: Sending {responsePayload.Response.Head.Description}");
                                },
                                onError: ex =>
                                {
                                    Console.Error.WriteLine($"Error with request {requestId}: {ex.Message}");
                                },
                                onCompleted: async () =>
                                {
                                    await Console.Out.WriteLineAsync($"Finished with request {requestId}");
                                    tcs.SetResult(true);
                                },
                                token: cts.Token);

                        _ = await tcs.Task; // only leave on onCompleted
                    },
                    onError: ex => Console.Error.WriteLine($"Error with EventHub: {ex.Message}"),
                    onCompleted: () => Console.WriteLine($"Finished with EventHub"),
                    token: cts.Token);

            await Console.In.ReadLineAsync();
            cts.Cancel();
        }

        private static IObservable<FashionItem> GetResponses()
        {
            var sufficientlyGoodHat = new FashionItem(size: 16, fashionType: FashionTypes.Hat, price: 12_00, description: "A nice large hat", stockKeepingUnitID: Guid.NewGuid().ToString());
            var sufficientlyGoodHatButTooExpensive = new FashionItem(size: 16, fashionType: FashionTypes.Hat, price: 12_50, description: "A very same nice large hat", stockKeepingUnitID: sufficientlyGoodHat.StockKeepingUnitID);
            var someThrouser = new FashionItem(size: 54, fashionType: FashionTypes.Throusers, price: 120_00, description: "A blue Jeans", stockKeepingUnitID: Guid.NewGuid().ToString());
            var aHatButTooSmall = new FashionItem(size: 15, fashionType: FashionTypes.Hat, price: 13_00, description: "A smaller hat", stockKeepingUnitID: Guid.NewGuid().ToString());
            var someDifferentHat = new FashionItem(size: 16, fashionType: FashionTypes.Hat, price: 12_00, description: "A different large hat", stockKeepingUnitID: Guid.NewGuid().ToString());

            return
                sufficientlyGoodHat.EmitIn(TimeSpan.FromSeconds(1))
                .And(someThrouser).In(TimeSpan.FromSeconds(2))
                .And(aHatButTooSmall).In(TimeSpan.FromSeconds(11))
                .And(someDifferentHat).In(TimeSpan.FromSeconds(1))
                .And(sufficientlyGoodHatButTooExpensive).In(TimeSpan.FromSeconds(1));
        }
    }
}