namespace SampleProvider
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Messaging.AzureImpl;
    using Microsoft.FSharp.Collections;

    internal class SampleProviderProgram
    {
        private static async Task Main()
        {
            Console.Title = "Sample Provider";

            var blobStorageServiceClient = new BlobContainerClient(
               blobContainerUri: new Uri($"https://{DemoCredential.StorageOffloadAccountName}.blob.core.windows.net/{DemoCredential.StorageOffloadContainerNameResponses}/"),
               credential: DemoCredential.AADServicePrincipal);

            var requestsClient = MessagingClients.Requests<SearchRequest>(partitionId: null);
            
            Func<string, AzureMessagingClient<SearchResponse>> responseTopic = 
                topicName => MessagingClients.Responses<SearchResponse>(topicName: topicName, partitionId: "0");

            var cts = new CancellationTokenSource();

            static string getBlobName(SearchRequest search, string requestId) => $"{requestId}/{Guid.NewGuid()}.json";

            requestsClient
                .CreateObervable(SeekPosition.Tail, cts.Token)
                .Subscribe(
                    onNext: async searchTuple =>
                        {
                            var search = searchTuple.Item2;
                            var requestId = search.RequestID;
                            var responseProducer = responseTopic(search.ResponseTopic);
                            Console.Out.WriteLine($"{requestId}: Somebody's looking for {search.Query.FashionType}");


                            var tcs = new TaskCompletionSource<bool>();

                            GetResponses()
                                .Select(foundFashionItem => new SearchResponsePayload(
                                    requestID: requestId,
                                    response: ListModule.OfArray(new[] { foundFashionItem })))
                                .Subscribe(
                                    onNext: async (responsePayload) =>
                                    {
                                        var blobName = getBlobName(search, requestId);

                                        var uploadInfo = await blobStorageServiceClient.UploadBlobAsync(
                                            blobName: blobName,
                                            content: responsePayload.AsJSONStream(),
                                            cancellationToken: cts.Token);

                                        await responseProducer.SendMessage(
                                            new SearchResponse(requestID: requestId, responseBlob: blobName),
                                            requestId: requestId);

                                        await Console.Out.WriteLineAsync($"{requestId}: Sending {responsePayload.Response.Head.Description} ({uploadInfo.Value.ETag})");
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
                                    token: cts.Token
                                );

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
