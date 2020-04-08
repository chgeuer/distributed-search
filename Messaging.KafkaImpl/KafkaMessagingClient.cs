namespace Messaging.KafkaImpl
{
    using System;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Credentials;
    using Interfaces;
    using LanguageExt;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;
    using ConfluentKafkaOffset = Confluent.Kafka.Offset;
    using Offset = Fundamentals.Types.Offset;

    internal class KafkaMessagingClient<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly IProducer<Null, string> producer;
        private readonly IConsumer<Ignore, string> consumer;
        private readonly TopicPartition topicPartition;

        public KafkaMessagingClient(TopicPartitionID topicPartitionID)
        {
            var bootstrapServers = $"{DemoCredential.EventHubName}.servicebus.windows.net:9093";
            var saslUsername = "$ConnectionString";
            var saslPassword = DemoCredential.EventHubConnectionString;
            var securityProtocol = SecurityProtocol.SaslSsl;
            var saslMechanism = SaslMechanism.Plain;
            var groupId = "$Default";

            this.producer = new ProducerBuilder<Null, string>(new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    SecurityProtocol = securityProtocol,
                    SaslMechanism = saslMechanism,
                    SaslUsername = saslUsername,
                    SaslPassword = saslPassword,

                    // SslCaLocation = cacertlocation,
                    // Debug = "security,broker,protocol",
                })
                .SetKeySerializer(Serializers.Null)
                .SetValueSerializer(Serializers.Utf8)
                .Build();

            this.consumer = new ConsumerBuilder<Ignore, string>(new ConsumerConfig
                {
                    BootstrapServers = bootstrapServers,
                    SecurityProtocol = securityProtocol,
                    SaslMechanism = saslMechanism,
                    SaslUsername = saslUsername,
                    SaslPassword = saslPassword,
                    GroupId = groupId,
                    BrokerVersionFallback = "1.0.0",
                    AutoOffsetReset = AutoOffsetReset.Latest,

                    // SslCaLocation = cacertlocation,
                    // Debug = "security,broker,protocol",
                })
                .SetKeyDeserializer(Deserializers.Ignore)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            bool useSpecificPartition = FSharpOption<int>.get_IsSome(topicPartitionID.PartitionId);
            var partition = useSpecificPartition
                ? new Partition(topicPartitionID.PartitionId.Value)
                : Partition.Any;

            this.topicPartition = new TopicPartition(
                    topic: topicPartitionID.TopicName,
                    partition: partition);
        }

        public Task<Offset> SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default)
            => this.SendMessage(messagePayload: messagePayload, requestId: null, cancellationToken);

        public async Task<Offset> SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default)
        {
            var kafkaMessage = new Message<Null, string>
            {
                Key = null,
                Value = messagePayload.AsJSON(),
                Headers = new Headers(),
            };

            if (!string.IsNullOrEmpty(requestId))
            {
                SetRequestID(kafkaMessage.Headers, requestId);
            }

            var report = await this.producer.ProduceAsync(
                topicPartition: this.topicPartition,
                message: kafkaMessage);

            // await Console.Out.WriteLineAsync($"TX {report.Topic}#{report.Partition.Value}#{report.Offset.Value} {messagePayload}");
            return Offset.NewOffset(report.Offset.Value);
        }

        public IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            return CreateObservable(
                    consumer: this.consumer,
                    tp: this.topicPartition,
                    startingPosition: startingPosition,
                    cancellationToken: cancellationToken)
                .Select(consumeResult => new Message<TMessagePayload>(
                    offset: Offset.NewOffset(consumeResult.Offset.Value),
                    requestID: GetRequestID(consumeResult.Message.Headers),
                    payload: consumeResult.Message.Value.DeserializeJSON<TMessagePayload>()));
        }

        private static IObservable<ConsumeResult<TKey, TValue>> CreateObservable<TKey, TValue>(
            IConsumer<TKey, TValue> consumer,
            TopicPartition tp,
            SeekPosition startingPosition,
            CancellationToken cancellationToken)
        {
            return Observable.Create<ConsumeResult<TKey, TValue>>(o =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _ = Task.Run(
                    () =>
                    {
                        ConfluentKafkaOffset confluentKafkaOffset = startingPosition switch
                        {
                            SeekPosition.FromOffset o => new ConfluentKafkaOffset(o.Offset.Item),
                            _ => ConfluentKafkaOffset.End,
                        };
                        var tpo = new TopicPartitionOffset(tp, confluentKafkaOffset);

                        if (tpo.Partition.Value == -1 && tpo.Offset.Value == -1)
                        {
                            consumer.Subscribe(topic: tp.Topic);
                        }
                        else
                        {
                            // Console.Out.WriteLine($"consumerAssign(topic={tpo.Topic} partition={tpo.Partition.Value} offset={tpo.Offset.Value})");
                            consumer.Assign(tpo);
                        }

                        while (!cts.Token.IsCancellationRequested)
                        {
                            var msg = consumer.Consume(cts.Token);

                            // Console.WriteLine($"RX {msg.Topic}#{msg.Partition.Value}#{msg.Offset.Value}: {msg.Message.Value}");
                            o.OnNext(msg);
                            cts.Token.ThrowIfCancellationRequested();
                        }

                        o.OnCompleted();
                    },
                    cts.Token);

                return new CancellationDisposable(cts);
            });
        }

        private static readonly string RequestIdPropertyName = "requestIDString";

        private static FSharpOption<string> GetRequestID(Headers headers)
        {
            return headers.TryGetLastBytes(RequestIdPropertyName, out var bytes)
                ? FSharpOption<string>.Some(bytes.ToUTF8String())
                : FSharpOption<string>.None;
        }

        private static void SetRequestID(Headers headers, string requestId)
            => headers.Add(RequestIdPropertyName, requestId.ToUTF8Bytes());
    }
}