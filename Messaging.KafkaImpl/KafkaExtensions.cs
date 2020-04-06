namespace Messaging.KafkaImpl
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Interfaces;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;

    internal static class KafkaExtensions
    {
        internal static IObservable<ConsumeResult<TKey, TValue>> CreateObservable<TKey, TValue>(
            this IConsumer<TKey, TValue> consumer,
            TopicPartition topicPartition,
            SeekPosition startingPosition,
            CancellationToken cancellationToken)
        {
            Console.Out.WriteLine($"1 Create observable topic \"{topicPartition.Topic}\" partition \"{topicPartition.Partition}\" startingPosition {startingPosition}");

            if (startingPosition.IsFromOffset)
            {
                long offset = ((SeekPosition.FromOffset)startingPosition).UpdateOffset.Item;
                Console.Out.WriteLine($"1 Seek \"{topicPartition.Topic}\" partition \"{topicPartition.Partition}\" offset {offset}");
                consumer.Assign(new TopicPartitionOffset(
                    tp: topicPartition,
                    offset: new Offset(offset)));
            }

            return Observable.Create<ConsumeResult<TKey, TValue>>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _ = Task.Factory.StartNew(
                    () =>
                    {
                        Console.WriteLine($"1 Starting Loop \"{topicPartition.Topic}\"");
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var msg = consumer.Consume(cts.Token);
                            Console.WriteLine($"1 Retrieved {msg.Offset.Value}");
                            o.OnNext(msg);

                            cts.Token.ThrowIfCancellationRequested();
                        }

                        o.OnCompleted();
                    },
                    cts.Token);

                return new CancellationDisposable(cts);
            });
        }

        internal static IObservable<ConsumeResult<TKey, TValue>> CreateObservable<TKey, TValue>(this IConsumer<TKey, TValue> consumer, CancellationToken cancellationToken = default)
        {
            Console.Out.WriteLine($"Create observable topic \"{consumer.ConsumerGroupMetadata}\"");

            return Observable.Create<ConsumeResult<TKey, TValue>>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _ = Task.Factory.StartNew(
                    () =>
                    {
                        Console.WriteLine($"2 Starting Loop");
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var msg = consumer.Consume(cts.Token);
                            Console.WriteLine($"2 Retrieved {msg.Offset.Value}");
                            o.OnNext(msg);

                            cts.Token.ThrowIfCancellationRequested();
                        }

                        o.OnCompleted();
                    },
                    cts.Token);

                return new CancellationDisposable(cts);
            });
        }

        internal static readonly string RequestIdPropertyName = "requestIDString";

        internal static FSharpOption<string> GetRequestID(this Headers headers)
        {
            return headers.TryGetLastBytes(RequestIdPropertyName, out var bytes)
                ? FSharpOption<string>.Some(bytes.ToUTF8String())
                : FSharpOption<string>.None;
        }

        internal static void SetRequestID(this Headers headers, string requestId)
            => headers.Add(RequestIdPropertyName, requestId.ToUTF8Bytes());
    }
}