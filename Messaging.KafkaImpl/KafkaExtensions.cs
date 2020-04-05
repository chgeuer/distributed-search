namespace Messaging.KafkaImpl
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using static Fundamentals.Types;

    internal static class KafkaExtensions
    {
        internal static IObservable<ConsumeResult<long, byte[]>> CreateObservable(
            this IConsumer<long, byte[]> consumer,
            TopicPartition topicPartition,
            SeekPosition startingPosition,
            CancellationToken cancellationToken)
        {
            if (!startingPosition.IsFromOffset)
            {
                consumer.Assign(topicPartition);
            }
            else
            {
                long offset = ((SeekPosition.FromOffset)startingPosition).UpdateOffset;
                consumer.Assign(new TopicPartitionOffset(
                    tp: topicPartition,
                    offset: new Offset(offset)));
            }

            return Observable.Create<ConsumeResult<long, byte[]>>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _ = Task.Run(
                    () =>
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var msg = consumer.Consume(cts.Token);
                            o.OnNext(msg);

                            cts.Token.ThrowIfCancellationRequested();
                        }

                        o.OnCompleted();
                    },
                    cts.Token);

                return new CancellationDisposable(cts);
            });
        }

        internal static IObservable<ConsumeResult<long, byte[]>> CreateObservable(this IConsumer<long, byte[]> consumer, CancellationToken cancellationToken = default)
        {
            return Observable.Create<ConsumeResult<long, byte[]>>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _ = Task.Run(
                    () =>
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var msg = consumer.Consume(cts.Token);
                            o.OnNext(msg);

                            cts.Token.ThrowIfCancellationRequested();
                        }

                        o.OnCompleted();
                    },
                    cts.Token);

                return new CancellationDisposable(cts);
            });
        }
    }
}