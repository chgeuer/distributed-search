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
    using Microsoft.FSharp.Collections;
    using static Fundamentals.Types;

    public class KafkaMessagingClient<TMessagePayload> : IMessageClient<TMessagePayload>
    {
        private readonly IProducer<long, byte[]> producer;
        private readonly IConsumer<long, byte[]> consumer;
        private readonly string topic;

        public KafkaMessagingClient(string topic)
        {
            var bootstrapServers = $"{DemoCredential.EventHubName}.servicebus.windows.net:9093";
            var saslUsername = "$ConnectionString";
            var saslPassword = DemoCredential.EventHubConnectionString;
            var securityProtocol = SecurityProtocol.SaslSsl;
            var saslMechanism = SaslMechanism.Plain;

            this.producer = new ProducerBuilder<long, byte[]>(new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    SecurityProtocol = securityProtocol,
                    SaslMechanism = saslMechanism,
                    SaslUsername = saslUsername,
                    SaslPassword = saslPassword,

                    // SslCaLocation = cacertlocation,
                    // Debug = "security,broker,protocol", //Uncomment for librdkafka debugging information
                })
                .SetKeySerializer(Serializers.Int64)
                .SetValueSerializer(Serializers.ByteArray)
                .Build();

            this.consumer = new ConsumerBuilder<long, byte[]>(new ConsumerConfig
                {
                    BootstrapServers = bootstrapServers,
                    SecurityProtocol = securityProtocol,
                    SaslMechanism = saslMechanism,
                    SaslUsername = saslUsername,
                    SaslPassword = saslPassword,

                    // GroupId = EventHubConsumerClient.DefaultConsumerGroupName,
                    // SslCaLocation = cacertlocation,
                    // Debug = "security,broker,protocol", //Uncomment for librdkafka debugging information
                })
                .SetKeyDeserializer(Deserializers.Int64)
                .SetValueDeserializer(Deserializers.ByteArray)
                .Build();

            this.topic = topic;
        }

        public IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default)
            => this.consumer
                .CreateObservable(cancellationToken: cancellationToken)
                .Select(consumeResult => new Message<TMessagePayload>(
                    offset: consumeResult.Offset.Value,
                    payload: consumeResult.Message.Value.AsJSON().DeserializeJSON<TMessagePayload>(),
                    properties: new FSharpMap<string, object>(
                        consumeResult.Message.Headers.Select(i =>
                            Tuple.Create(i.Key, (object)i.GetValueBytes())))));

        public async Task<long> SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default)
        {
            var report = await this.producer.ProduceAsync(
                topic: this.topic,
                message: new Message<long, byte[]>
                {
                    Key = DateTime.UtcNow.Ticks,
                    Value = messagePayload.AsJSON().ToUTF8Bytes(),
                });

            return report.Offset.Value;
        }

        public Task<long> SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}