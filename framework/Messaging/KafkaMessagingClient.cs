namespace Mercury.Messaging
{
    using System;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Mercury.Fundamentals;
    using Mercury.Interfaces;
    using Mercury.Utils.Extensions;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;
    using ConfluentKafkaOffset = Confluent.Kafka.Offset;
    using ConfluentPartition = Confluent.Kafka.Partition;
    using MercuryOffset = Mercury.Fundamentals.Types.Watermark;
    using MercuryPartition = Mercury.Fundamentals.Types.Partition;

    // A purely internal implementation dealing with Kafka. No Confluent data types outside this file and on public APIs.
    internal class KafkaMessagingClient<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly IProducer<Null, string> producer;
        private readonly IConsumer<Ignore, string> consumer;
        private readonly IAdminClient adminClient;
        private readonly Lazy<TopicPartition> topicPartition;

        public KafkaMessagingClient(IDistributedSearchConfiguration demoCredential, TopicAndPartition topicAndPartition)
        {
            var bootstrapServers = $"{demoCredential.EventHubName}.servicebus.windows.net:9093";
            var saslUsername = "$ConnectionString";
            var saslPassword = demoCredential.EventHubConnectionString;
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

            this.adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrapServers,
                SecurityProtocol = securityProtocol,
                SaslMechanism = saslMechanism,
                SaslUsername = saslUsername,
                SaslPassword = saslPassword,
            }).Build();

            this.topicPartition = new Lazy<TopicPartition>(() => new TopicPartition(
                    topic: topicAndPartition.TopicName,
                    partition: DeterminePartitionID(this.adminClient, topicAndPartition)));
        }

        public Task<MercuryOffset> SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default)
            => this.SendMessage(messagePayload: messagePayload, requestId: null, cancellationToken);

        public async Task<MercuryOffset> SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default)
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
                topicPartition: this.topicPartition.Value,
                message: kafkaMessage);

            await Console.Out.WriteLineAsync($"Sent {report.Topic}#{report.Partition.Value}#{report.Offset.Value} {messagePayload}");
            return MercuryOffset.NewWatermark(report.Offset.Value);
        }

        public IObservable<WatermarkMessage<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
        {
            return CreateObservable(
                    consumer: this.consumer,
                    tp: this.topicPartition.Value,
                    startingPosition: startingPosition,
                    cancellationToken: cancellationToken)
                .Select(consumeResult => new WatermarkMessage<TMessagePayload>(
                    watermark: MercuryOffset.NewWatermark(consumeResult.Offset.Value),
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
                var innerCancellationToken = cts.Token;

                _ = Task.Run(
                    () =>
                    {
                        ConfluentKafkaOffset confluentKafkaOffset = startingPosition switch
                        {
                            SeekPosition.FromWatermark o => new ConfluentKafkaOffset(o.Watermark.Item),
                            _ => ConfluentKafkaOffset.End,
                        };
                        var tpo = new TopicPartitionOffset(tp, confluentKafkaOffset);

                        if (tpo.Partition.Value == -1 && tpo.Offset.Value == -1)
                        {
                            consumer.Subscribe(topic: tp.Topic);
                        }
                        else
                        {
                            Console.Out.WriteLine($"consumerAssign(topic={tpo.Topic} partition={tpo.Partition.Value} offset={tpo.Offset.Value})");
                            consumer.Assign(tpo);
                        }

                        while (!innerCancellationToken.IsCancellationRequested)
                        {
                            var msg = consumer.Consume(innerCancellationToken);

                            Console.WriteLine($"Received {msg.Topic}#{msg.Partition.Value}#{msg.Offset.Value}: {msg.Message.Value}");
                            o.OnNext(msg);
                            innerCancellationToken.ThrowIfCancellationRequested();
                        }

                        o.OnCompleted();
                    },
                    innerCancellationToken);

                return new CancellationDisposable(cts);
            });
        }

        private static Func<string, FSharpOption<int>> GetPartitionCount(IAdminClient adminClient) => (string topicName) =>
        {
            var metadata = adminClient.GetMetadata(
                topic: topicName,
                timeout: TimeSpan.FromSeconds(10));

            var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic == topicName);
            if (topicMetadata == null)
            {
                return FSharpOption<int>.None;
            }

            return FSharpOption<int>.Some(topicMetadata.Partitions.Count);
        };

        private static ConfluentPartition DeterminePartitionID(IAdminClient adminClient, TopicAndPartition topicAndPartition)
        {
            MercuryPartition partitionId = determinePartitionID(
                determinePartitionCount: GetPartitionCount(adminClient).ToFSharpFunc(),
                topicAndPartition: topicAndPartition);

            return partitionId switch
            {
                MercuryPartition.Partition x => new ConfluentPartition(x.Item),
                _ => ConfluentPartition.Any,
            };
        }

        private const string RequestIdPropertyName = "requestIDString";

        private static FSharpOption<string> GetRequestID(Headers headers)
            => headers.TryGetLastBytes(RequestIdPropertyName, out var bytes)
                ? FSharpOption<string>.Some(bytes.ToUTF8String())
                : FSharpOption<string>.None;

        private static void SetRequestID(Headers headers, string requestId)
            => headers.Add(RequestIdPropertyName, requestId.ToUTF8Bytes());
    }
}