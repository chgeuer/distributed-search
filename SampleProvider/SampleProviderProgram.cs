namespace SampleProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Messaging;
    using Microsoft.FSharp.Collections;
    using static Fundamentals.Types;

    internal class SampleProviderProgram
    {
        private static async Task Main()
        {
            Console.Title = "Sample Provider";

            var requestsClient = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>();

            var cts = new CancellationTokenSource();

            var clients = new Dictionary<ResponseTopicAddress, IMessageClient<ProviderSearchResponse<FashionItem>>>();
            IMessageClient<ProviderSearchResponse<FashionItem>> getMessageClient(ResponseTopicAddress rta)
            {
                lock (clients)
                {
                    if (!clients.ContainsKey(rta))
                    {
                        var client = MessagingClients.WithStorageOffload<ProviderSearchResponse<FashionItem>>(
                                responseTopicAddress: rta,
                                accountName: DemoCredential.StorageOffloadAccountName,
                                containerName: DemoCredential.StorageOffloadContainerNameResponses);
                        clients.Add(rta, client);
                    }
                }
                return clients[rta];
            }

            requestsClient
                .CreateObervable(SeekPosition.FromTail, cts.Token)
                .Subscribe(
                    onNext: async providerSearchRequestMessage =>
                    {
                        var search = providerSearchRequestMessage.Payload;
                        var requestId = providerSearchRequestMessage.RequestID;

                        var responseProducer = getMessageClient(search.ResponseTopic);

                        // await Console.Out.WriteLineAsync($"{requestId}: Somebody's looking for {search.SearchRequest.FashionType}");

                        var tcs = new TaskCompletionSource<bool>();

                        GetResponses()
                            .Select(foundFashionItem => new ProviderSearchResponse<FashionItem>(
                                requestID: requestId.Value,
                                response: ListModule.OfArray(new[] { foundFashionItem })))
                            .Subscribe(
                                onNext: async (responsePayload) =>
                                {
                                    await responseProducer.SendMessage(
                                        messagePayload: responsePayload,
                                        requestId: requestId.Value,
                                        cancellationToken: cts.Token);

                                    // await Console.Out.WriteLineAsync($"{requestId}: Sending {responsePayload.Response.Head.Description}");
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
                sufficientlyGoodHat.EmitIn(TimeSpan.FromSeconds(0.8))
                .And(someThrouser).In(TimeSpan.FromSeconds(0.9))
                .And(aHatButTooSmall).In(TimeSpan.FromSeconds(1))
                .And(someDifferentHat).In(TimeSpan.FromSeconds(1.1))
                .And(sufficientlyGoodHatButTooExpensive).In(TimeSpan.FromSeconds(1.2));
        }
    }
}