namespace Messaging.AzureImpl
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
    using Interfaces;
    using LanguageExt;
    using static LanguageExt.Prelude;

    internal static class EventHubExtensions
    {
        internal static IObservable<PartitionEvent> CreateObservable(
            this EventHubConsumerClient eventHubConsumerClient,
            string partitionId,
            EventPosition startingPosition,
            CancellationToken cancellationToken = default)
        {
            return eventHubConsumerClient
                .ReadEventsFromPartitionAsync(
                    partitionId: partitionId,
                    startingPosition: startingPosition,
                    readOptions: new ReadEventOptions { },
                    cancellationToken: cancellationToken)
                .CreateObservable(cancellationToken);
        }

        internal static IObservable<PartitionEvent> CreateObservable(
            this EventHubConsumerClient eventHubConsumerClient,
            CancellationToken cancellationToken = default)
        {
            return eventHubConsumerClient
                .ReadEventsAsync(
                    startReadingAtEarliestEvent: false,
                    readOptions: new ReadEventOptions { },
                    cancellationToken: cancellationToken)
                .CreateObservable(cancellationToken);
        }

        internal static IObservable<PartitionEvent> CreateObservable(this IAsyncEnumerable<PartitionEvent> events, CancellationToken cancellationToken = default)
        {
            return Observable.Create<PartitionEvent>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

        internal static string GetBodyAsUTF8(this EventData eventData)
        {
            using var ms = new MemoryStream();
            eventData.BodyAsStream.CopyTo(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        internal static Option<T> GetProperty<T>(this EventData record, string fieldName) =>
            record.Properties.TryGetValue(fieldName, out object result) ? Some((T)result) : None;

        internal static EventPosition AsEventPosition(this SeekPosition position)
            => position.FromTail
                ? EventPosition.Latest
                : EventPosition.FromOffset(offset: position.Offset, isInclusive: false);
    }
}