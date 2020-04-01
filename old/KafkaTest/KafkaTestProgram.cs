namespace KafkaTest
{
    using Avro.File;
    using Avro.Generic;
    using Azure.Core;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Producer;
    using Confluent.Kafka;
    using Credentials;
    using Interfaces;
    using Messaging.AzureImpl;
    using LanguageExt;
    using StackExchange.Redis;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using static LanguageExt.Prelude;

    internal class Foo
    {
        public int RequestID { get; set; }
        public string[] Bars { get; set; }
    }

    internal class KafkaTestProgram
    {
        private static async Task Main()
        {
            //var b = AvroConvert.Serialize(new Foo { RequestID = 15, Bars = new[] { "fuck", "foo" } });
            //File.WriteAllBytes("1.avro", b);

            //var f = AvroConvert.Deserialize<Foo>(b);
            //Console.WriteLine(f.Bars[0]);

            var (eventHubName, entityPath) = (DemoCredential.EventHubName, DemoCredential.EntityPath);

            //var connectionString = $"Endpoint=sb://{eventHubName}.servicebus.windows.net/;SharedAccessKeyName={sharedAccessKeyName};SharedAccessKey={sharedAccessKeyValue};EntityPath={entityPath}";
            //var rootConnectionString = (string)projectConfig.sharedAccessConnectionStringRoot;
            //var brokerList = $"{eventHubName}.servicebus.windows.net:9093";
            var topic = entityPath;

            await AzureEventHubRoundtripAsync(
                fullyQualifiedNamespace: $"{eventHubName}.servicebus.windows.net",
                eventHubName: topic,
                credential: DemoCredential.AADServicePrincipal);

            //await Console.Out.WriteLineAsync("Initializing Producer");
            //await ConfluentProducer(
            //    brokerList: brokerList,
            //    connectionString: connectionString,
            //    topic: topic);

            //await Console.Out.WriteLineAsync("Initializing Consumer");
            //ConfluentConsumer(
            //    brokerList: brokerList, 
            //    connectionString: connectionString, 
            //    consumergroup: EventHubConsumerClient.DefaultConsumerGroupName, 
            //    topic: topic);

            await Console.In.ReadLineAsync();
        }

        public static async Task AzureEventHubRoundtripAsync(string fullyQualifiedNamespace, string eventHubName, TokenCredential credential)
        {
            // https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs/README.md
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            using var redis = ConnectionMultiplexer.Connect("localhost");

            static string determinePartitionId(string[] ids) => ids[0];

            //
            // Consume
            //
            await Console.Out.WriteLineAsync($"Reading from \"{fullyQualifiedNamespace}/{eventHubName}\"");

            await using var consumerClient = new EventHubConsumerClient(
                consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                fullyQualifiedNamespace: fullyQualifiedNamespace,
                eventHubName: eventHubName,
                credential: credential);

            static EventPosition determineEventPartition() =>
                // EventPosition.FromEnqueuedTime(DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(20)));
                // EventPosition.FromOffset(offset: 0, isInclusive: true);
                EventPosition.Latest;

            var consumerPartitionId = determinePartitionId(await consumerClient.GetPartitionIdsAsync(cancellationToken));
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

            IObservable<PartitionEvent> eventHubObservable = consumerClient.CreateObservable(
                // partitionId: consumerPartitionId,
                // startingPosition: determineEventPartition(),
                // readOptions: new ReadEventOptions { },
                cancellationToken: cancellationToken);

            using var eventHubSubscription = eventHubObservable.Subscribe(new RedisObserver(redis));

            using var redisSubscription = new RedisObservable(redis, channelName: "kafka")
                .Select(redisValue => (byte[])redisValue)
                .Select(bytes => $"{bytes.Length} ")
                .Subscribe(new ConsoleObserver<string>(x => x));


            ////
            //// Produce
            ////
            await Console.Out.WriteLineAsync($"Sending to \"{fullyQualifiedNamespace}/{eventHubName}\"");
            await using EventHubProducerClient producer = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credential);
            var producerPartitionId = determinePartitionId(await producer.GetPartitionIdsAsync(cancellationToken));

            Random r = new Random();
            string createData(int len)
            {
                var result = new byte[len];
                r.NextBytes(result);
                return Convert.ToBase64String(result);
            }

            for (var i = 0; ; i++)
            {
                EventData create(string s)
                {
                    var eventData = new EventData(s.ToUTF8Bytes());
                    var requestID = Guid.NewGuid();
                    eventData.Properties.Add("requestIDString", requestID.ToString());
                    eventData.Properties.Add("currentMachineTimeUTC", DateTime.UtcNow);
                    return eventData;
                }

                using EventDataBatch batchOfOne = await producer.CreateBatchAsync(
                    options: new CreateBatchOptions { PartitionId = producerPartitionId },
                    cancellationToken: cancellationToken);

                batchOfOne.TryAdd(create(createData(512 * 1024)));
                batchOfOne.TryAdd(create(createData(2 * 1024)));
                batchOfOne.TryAdd(create($"Hello {i}-A"));
                batchOfOne.TryAdd(create($"Hello {i}-B"));
                batchOfOne.TryAdd(create($"Hello {i}-C"));
                batchOfOne.TryAdd(create($"Hello {i}-D"));
                batchOfOne.TryAdd(create($"Hello {i}-E"));

                await producer.SendAsync(
                    eventBatch: batchOfOne,
                    cancellationToken: cancellationToken);
            }
        }

        public static async Task AvroPlayground()
        {
            // https://stackoverflow.com/questions/39846833/deserialize-an-avro-file-with-c-sharp
            var file = @"C:\Users\chgeuer\Desktop\chgp-0-2020-02-13-21-06-08.avro";
            using var stream = File.OpenRead(file);
            var dataFileReader = DataFileReader<GenericRecord>.OpenReader(stream);
            await Console.Out.WriteLineAsync(dataFileReader.GetSchema().ToString());
            var d = dataFileReader.Next();
            //foreach (var record in dataFileReader.NextEntries)
            //{
            //    await Console.Out.WriteLineAsync($"-> {record.Schema}");
            //    break;
            //}
        }

        public static async Task ConfluentProducer(string brokerList, string connectionString, string topic)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = brokerList,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "$ConnectionString",
                SaslPassword = connectionString,
                // SslCaLocation = cacertlocation,
                // Debug = "security,broker,protocol", //Uncomment for librdkafka debugging information

            };

            using var producer = new ProducerBuilder<long, string>(config)
                .SetKeySerializer(Serializers.Int64)
                .SetValueSerializer(Serializers.Utf8)
                .Build();

            Console.WriteLine("Sending 10 messages to topic: " + topic + ", broker(s): " + brokerList);
            for (int x = 0; x < 100; x++)
            {
                var msg = $"Sample message #{x} sent at {DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ffff")}";
                var deliveryReport = await producer.ProduceAsync(topic, new Message<long, string> { Key = DateTime.UtcNow.Ticks, Value = msg });
                Console.WriteLine($"Message {x} sent (value: '{msg}') offset {deliveryReport.Offset} status {deliveryReport.Status} topicPartitionOffset {deliveryReport.TopicPartitionOffset}");
            }
        }

        public static void ConfluentConsumer(string brokerList, string connectionString, string consumergroup, string topic)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = brokerList,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "$ConnectionString",
                SaslPassword = connectionString,
                // SslCaLocation = cacertlocation,
                SessionTimeoutMs = (int)TimeSpan.FromSeconds(30).TotalMilliseconds,
                // SocketTimeoutMs = (int)TimeSpan.FromSeconds(60).TotalMilliseconds,
                GroupId = consumergroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                BrokerVersionFallback = "1.0.0",
                // Debug = "security,broker,protocol",  //Uncomment for librdkafka debugging information
            };

            using var consumer = new ConsumerBuilder<long, string>(config)
                .SetKeyDeserializer(Deserializers.Int64)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
            consumer.Subscribe(topic);

            Console.WriteLine("Consuming messages from topic: " + topic + ", broker(s): " + brokerList);
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var msg = consumer.Consume(cts.Token);
                    Console.WriteLine($"Received: '{msg.Value}'");
                }
                catch (OperationCanceledException) { }
            }
        }
    }

    public class ConsoleObserver<T> : IObserver<T>
    {
        private readonly Func<T, string> toStringLambda;

        public ConsoleObserver(Func<T, string> toStringLambda)
        {
            this.toStringLambda = toStringLambda;
        }

        void IObserver<T>.OnCompleted()
        {
            Console.WriteLine("Done...");
        }

        void IObserver<T>.OnError(Exception error)
        {
            Console.Error.WriteLine($"Error {error.Message}");
        }

        void IObserver<T>.OnNext(T t)
        {
            Console.Out.Write(toStringLambda(t));
        }
    }

    public class RedisObserver : IObserver<PartitionEvent>
    {
        private readonly IDatabase Database;

        public RedisObserver(ConnectionMultiplexer redis)
        {
            this.Database = redis.GetDatabase();
        }

        void IObserver<PartitionEvent>.OnCompleted() { }

        void IObserver<PartitionEvent>.OnError(Exception error) { }

        void IObserver<PartitionEvent>.OnNext(PartitionEvent partitionEvent)
        {
            var eventData = partitionEvent.Data;
            var requestId = eventData.GetProperty<string>("requestIDString");
            var currentMachineTime = eventData.GetProperty<DateTime>("currentMachineTimeUTC");
            var val = $"RX Read: requestID {requestId} (\"{currentMachineTime}\") sequence# {eventData.SequenceNumber}  offset {eventData.Offset} EnqueuedTime {eventData.EnqueuedTime.ToLocalTime()} {eventData.Body.ToArray().ToUTF8String()}";

            Func<EventData, string> channelNameFromRequest = request => "kafka";

            Database.Publish(
                channel: new RedisChannel(
                    value: channelNameFromRequest(eventData),
                    mode: RedisChannel.PatternMode.Literal),
                message: RedisValue.CreateFrom(new MemoryStream(val.ToUTF8Bytes())),
                flags: CommandFlags.FireAndForget);
        }
    }

    public class RedisObservable : ObservableBase<RedisValue>, IDisposable
    {
        private readonly ConnectionMultiplexer _connection;
        private readonly ISubscriber _subscription;
        private readonly List<IObserver<RedisValue>> _observerList = new List<IObserver<RedisValue>>();

        public RedisObservable(ConnectionMultiplexer multiplexer, string channelName)
        {
            _connection = multiplexer;
            _subscription = multiplexer.GetSubscriber();
            void handler(RedisChannel channel, RedisValue value) => _observerList.AsParallel().ForAll(item => item.OnNext(value));
            _subscription.Subscribe(channelName, handler);
        }

        protected override IDisposable SubscribeCore(IObserver<RedisValue> observer)
        {
            _observerList.Add(observer);
            return Disposable.Create(() => { _observerList.Remove(observer); });
        }

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _connection.Dispose();
                    _observerList.AsParallel().ForAll(item => item.OnCompleted());
                }
            }
            disposed = true;
        }
    }

    public static class AvroExtensions
    {
        public static Option<T> GetValue<T>(this GenericRecord record, string fieldName) =>
            record.TryGetValue(fieldName, out object result) ? Some((T)result) : None;
    }

    public struct AvroEventData
    {
        public AvroEventData(dynamic record)
        {
            SequenceNumber = (long)record.SequenceNumber;
            Offset = (string)record.Offset;
            DateTime.TryParse((string)record.EnqueuedTimeUtc, out var enqueuedTimeUtc);
            EnqueuedTimeUtc = enqueuedTimeUtc;
            SystemProperties = (Dictionary<string, object>)record.SystemProperties;
            Properties = (Dictionary<string, object>)record.Properties;
            Body = (byte[])record.Body;
        }
        public long SequenceNumber { get; set; }
        public string Offset { get; set; }
        public DateTime EnqueuedTimeUtc { get; set; }
        public Dictionary<string, object> SystemProperties { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public byte[] Body { get; set; }
    }
}