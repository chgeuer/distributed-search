namespace Messaging.KafkaImpl
{
    using Confluent.Kafka;
    using Credentials;
    using Interfaces;

    public class KafkaMessagingClient
    {
        private readonly IProducer<long, string> producer;
        private readonly IConsumer<long, string> consumer;

        public KafkaMessagingClient()
        {
            this.producer = new ProducerBuilder<long, string>(new ProducerConfig
                {
                    BootstrapServers = $"{DemoCredential.EventHubName}.servicebus.windows.net:9093",
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = "$ConnectionString",
                    SaslPassword = DemoCredential.EventHubConnectionString,

                    // SslCaLocation = cacertlocation,
                    // Debug = "security,broker,protocol", //Uncomment for librdkafka debugging information
                })
                .SetKeySerializer(Serializers.Int64)
                .SetValueSerializer(Serializers.Utf8)
                .Build();

            this.consumer = new ConsumerBuilder<long, string>(new ConsumerConfig
                {
                    BootstrapServers = $"{DemoCredential.EventHubName}.servicebus.windows.net:9093",
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = "$ConnectionString",
                    SaslPassword = DemoCredential.EventHubConnectionString,

                    // GroupId = EventHubConsumerClient.DefaultConsumerGroupName,
                    // SslCaLocation = cacertlocation,
                    // Debug = "security,broker,protocol", //Uncomment for librdkafka debugging information
                })
                .SetKeyDeserializer(Deserializers.Int64)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();
        }
    }
}
