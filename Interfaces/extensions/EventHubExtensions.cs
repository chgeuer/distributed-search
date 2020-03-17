namespace Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Producer;
    using LanguageExt;
    using static LanguageExt.Prelude;

    public static class EventHubExtensions
    {
        public static object JsonConvert { get; private set; }

        public static IObservable<PartitionEvent> CreateObservable(
            this EventHubConsumerClient eventHubConsumerClient,
            CancellationToken cancellationToken = default)
        {
            return Observable.Create<PartitionEvent>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                IAsyncEnumerable<PartitionEvent> events = eventHubConsumerClient.ReadEventsAsync(
                    startReadingAtEarliestEvent: false,
                    readOptions: new ReadEventOptions(),
                    cancellationToken: cancellationToken);

                _ = Task.Run(
                    async () =>
                    {
                        await foreach (var e in events)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            o.OnNext(e);
                        }

                        o.OnCompleted();
                    },
                    cts.Token);

                return new CancellationDisposable(cts);
            });
        }

        public static async Task SendJsonRequest<T>(this EventHubProducerClient eventHubProducerClient, T item, string requestId)
        {
            byte[] serializedRequest = item.AsJSON().ToUTF8Bytes();
            var eventData = new EventData(eventBody: serializedRequest);
            eventData.Properties.Add("requestIDString", requestId);
            eventData.Properties.Add("currentMachineTimeUTC", DateTime.UtcNow);

            using EventDataBatch batchOfOne = await eventHubProducerClient.CreateBatchAsync();
            batchOfOne.TryAdd(eventData);

            await eventHubProducerClient.SendAsync(batchOfOne);
        }

        public static string GetBodyAsUTF8(this EventData eventData)
        {
            using var ms = new MemoryStream();
            eventData.BodyAsStream.CopyTo(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static Option<T> GetProperty<T>(this EventData record, string fieldName) =>
            record.Properties.TryGetValue(fieldName, out object result) ? Some((T)result) : None;
    }
}