namespace Mercury.Messaging
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Mercury.Interfaces;
    using Mercury.Utils.Extensions;
    using Microsoft.Azure.ServiceBus;
    using static Mercury.Fundamentals.Types;

    internal class ServiceBusMessagingClient<TMessagePayload> : IRequestResponseMessageClient<TMessagePayload>
    {
        private readonly IDistributedSearchConfiguration configuration;

        private readonly TopicClient topicClient;

        private readonly SubscriptionClient subscriptionClient;

        private static byte[] BytesFromPayLoad(TMessagePayload messagePayload) => messagePayload.AsJSON().ToUTF8Bytes();

        private static TMessagePayload PayLoadFromBytes(byte[] bytes) => bytes.ToUTF8String().DeserializeJSON<TMessagePayload>();

        private static void SetRequestIDOnMessage(Message message, string requestID) => message.UserProperties.Add("RequestID", requestID);

        private static string GetRequestIDFromMessage(Message message) => message.UserProperties["RequestID"] as string;

        public ServiceBusMessagingClient(IDistributedSearchConfiguration demoCredential)
        {
            this.configuration = demoCredential;

            this.topicClient = new TopicClient(
                connectionString: demoCredential.ServiceBusConnectionString,
                entityPath: demoCredential.ServiceBusTopicNameRequests);

            this.subscriptionClient = new SubscriptionClient(
                connectionString: demoCredential.ServiceBusConnectionString,
                topicPath: demoCredential.ServiceBusTopicNameRequests,
                subscriptionName: demoCredential.ProviderName,
                receiveMode: ReceiveMode.ReceiveAndDelete);
        }

        Task IRequestResponseMessageClient<TMessagePayload>.SendRequestResponseMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken)
        {
            var bytes = BytesFromPayLoad(messagePayload);
            var requestMessage = new Message(body: bytes);
            SetRequestIDOnMessage(requestMessage, requestId);
            return this.topicClient.SendAsync(requestMessage);
        }

        IObservable<RequestResponseMessage<TMessagePayload>> IRequestResponseMessageClient<TMessagePayload>.CreateObervable(CancellationToken cancellationToken)
        {
            // var managementClient = new ManagementClient(
            //    connectionString: this.configuration.ServiceBusConnectionString);
            // managementClient.CreateSubscriptionAsync(
            //    subscriptionDescription: new SubscriptionDescription(
            //        topicPath: this.configuration.ServiceBusTopicNameRequests,
            //        subscriptionName: this.configuration.ProviderName)
            //    {
            //        MaxDeliveryCount = 3,
            //    },
            //    cancellationToken: cancellationToken).Wait();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var innerCancellationToken = cts.Token;

            innerCancellationToken.Register(() =>
            {
                Console.Error.WriteLine("Calling this.subscriptionClient.CloseAsync().Wait()");

                this.subscriptionClient.CloseAsync().Wait();
            });

            return Observable.Create<RequestResponseMessage<TMessagePayload>>(observer =>
            {
                Task Handle(Message serviceBusMessage, CancellationToken ct)
                {
                    var requestID = GetRequestIDFromMessage(serviceBusMessage);
                    var payload = PayLoadFromBytes(serviceBusMessage.Body);

                    observer.OnNext(new RequestResponseMessage<TMessagePayload>(payload, requestID));

                    return Task.CompletedTask;
                }

                Task HandleError(ExceptionReceivedEventArgs args)
                {
                    observer.OnError(args.Exception);

                    return Task.CompletedTask;
                }

                this.subscriptionClient.RegisterMessageHandler(
                    handler: Handle,
                    messageHandlerOptions: new MessageHandlerOptions(exceptionReceivedHandler: HandleError)
                        {
                            MaxConcurrentCalls = 1,
                        });

                return new CancellationDisposable(cts);
            });
        }
    }
}