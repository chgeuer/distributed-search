namespace Mercury.Messaging;

using Azure.Messaging.ServiceBus;
using Mercury.Interfaces;
using Mercury.Utils.Extensions;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Mercury.Fundamentals.Types;

internal class ServiceBusMessagingClient<TMessagePayload> : IRequestResponseMessageClient<TMessagePayload>
{
    private readonly IDistributedSearchConfiguration configuration;

    private readonly ServiceBusClient serviceBusClient;

    private readonly ServiceBusSender topicClient;

    private readonly ServiceBusProcessor subscriptionClient;

    private static byte[] BytesFromPayLoad(TMessagePayload messagePayload) => messagePayload.AsJSON().ToUTF8Bytes();

    private static TMessagePayload PayLoadFromBytes(byte[] bytes) => bytes.ToUTF8String().DeserializeJSON<TMessagePayload>();

    private static void SetRequestIDOnMessage(ServiceBusMessage message, string requestID) => message.ApplicationProperties.Add("RequestID", requestID);

    private static string GetRequestIDFromMessage(ServiceBusReceivedMessage message) => message.ApplicationProperties["RequestID"] as string;

    public ServiceBusMessagingClient(IDistributedSearchConfiguration demoCredential)
    {
        this.configuration = demoCredential;

        this.serviceBusClient = new ServiceBusClient(
            connectionString: demoCredential.ServiceBusConnectionString);

        this.topicClient = this.serviceBusClient.CreateSender(
            queueOrTopicName: demoCredential.ServiceBusTopicNameRequests);

        this.subscriptionClient = this.serviceBusClient.CreateProcessor(
            topicName: demoCredential.ServiceBusTopicNameRequests,
            subscriptionName: demoCredential.ProviderName,
            options: new ServiceBusProcessorOptions
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
            });
    }

    Task IRequestResponseMessageClient<TMessagePayload>.SendRequestResponseMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken)
    {
        var bytes = BytesFromPayLoad(messagePayload);
        var requestMessage = new ServiceBusMessage(body: bytes);
        SetRequestIDOnMessage(requestMessage, requestId);
        return this.topicClient.SendMessageAsync(requestMessage);
    }

    IObservable<RequestResponseMessage<TMessagePayload>> IRequestResponseMessageClient<TMessagePayload>.CreateObervable(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var innerCancellationToken = cts.Token;

        innerCancellationToken.Register(() =>
        {
            this.subscriptionClient.CloseAsync().Wait();
        });

        return Observable.Create<RequestResponseMessage<TMessagePayload>>(observer =>
        {
            Task Handle(ProcessMessageEventArgs args)
            {
                var requestID = GetRequestIDFromMessage(args.Message);
                var payload = PayLoadFromBytes(args.Message.Body.ToArray());

                observer.OnNext(new RequestResponseMessage<TMessagePayload>(payload, requestID));

                return Task.CompletedTask;
            }

            Task HandleError(ProcessErrorEventArgs args)
            {
                observer.OnError(args.Exception);

                return Task.CompletedTask;
            }

            this.subscriptionClient.ProcessMessageAsync += Handle;
            this.subscriptionClient.ProcessErrorAsync += HandleError;

            return new CancellationDisposable(cts);
        });
    }
}