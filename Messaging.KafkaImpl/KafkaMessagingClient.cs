namespace Messaging.KafkaImpl
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Credentials;
    using Interfaces;
    using LanguageExt;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;

    public class KafkaMessagingClient<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly IProducer<Null, string> producer;
        private readonly IConsumer<Ignore, string> consumer;
        private readonly TopicPartition topicPartition;

        public KafkaMessagingClient(ResponseTopicAddress responseTopicAddress)
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

            var partition = FSharpOption<int>.get_IsSome(responseTopicAddress.PartitionId)
                ? new Partition(responseTopicAddress.PartitionId.Value)
                : Partition.Any;

            this.topicPartition = new TopicPartition(
                    topic: responseTopicAddress.TopicName,
                    partition: partition);
        }

        public IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
            => this.consumer
                .CreateObservable(
                    topicPartition: this.topicPartition,
                    startingPosition: startingPosition,
                    cancellationToken: cancellationToken)
                .Select(consumeResult => new Message<TMessagePayload>(
                    offset: UpdateOffset.NewUpdateOffset(consumeResult.Offset.Value),
                    requestID: consumeResult.Message.Headers.GetRequestID(),
                    payload: consumeResult.Message.Value.DeserializeJSON<TMessagePayload>()));

        public async Task<UpdateOffset> SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default)
        {
            var report = await this.producer.ProduceAsync(
                topic: this.topicPartition.Topic,
                message: new Message<Null, string>
                {
                    Key = null,
                    Value = messagePayload.AsJSON(),
                });

            return UpdateOffset.NewUpdateOffset(report.Offset.Value);
        }

        public async Task<UpdateOffset> SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default)
        {
            var kafkaMessage = new Message<Null, string>
            {
                Key = null,
                Value = messagePayload.AsJSON(),
                Headers = new Headers(),
            };

            kafkaMessage.Headers.SetRequestID(requestId);

            var report = await this.producer.ProduceAsync(
                topic: this.topicPartition.Topic,
                message: kafkaMessage);

            return UpdateOffset.NewUpdateOffset(report.Offset.Value);
        }
    }
}