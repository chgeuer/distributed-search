namespace businessLogicSpeedup
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;
    using StackExchange.Redis;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;

    #region RemoveNonSirectFlights

    public class RemoveNonDirectFlightsOriginal : IFlightProcessor
    {
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context)
        {
            return context.IsDirectFlight
                ? flights.Where(f => IsDirectLeg(f.OutboundLeg) && (context.OneWayOnly || IsDirectLeg(f.InboundLeg)))
                : flights;
        }

        private bool IsDirectLeg(Leg leg)
        {
            return leg != null && leg.Segments != null && leg.Segments.Count == 1 && leg.Segments.First().AdditionalStops == 0;
        }
    }

    public class RemoveNonDirectFlightsParallel : IFlightProcessor
    {
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context)
        {
            if (!context.IsDirectFlight) return flights;
            var oneWay = context.OneWayOnly;
            return flights
                .AsParallel()
                .Where(f => IsDirectLeg(f.OutboundLeg) && (oneWay || IsDirectLeg(f.InboundLeg)));
        }

        private static bool IsDirectLeg(Leg leg)
        {
            return leg != null &&
                leg.Segments != null &&
                leg.Segments.Count == 1 &&
                leg.Segments[0].AdditionalStops == 0;
        }
    }

    public class RemoveNonDirectFlightsLambda : IFlightProcessor, IObservableFlightProcessor
    {
        public static Func<Flight, bool> Filter(AvailabilitySearch context)
        {
            var allowIndirectFlights = !context.IsDirectFlight;
            var oneWayOnly = context.OneWayOnly;
            return f => allowIndirectFlights || (f.OutboundLeg.IsDirect() && (oneWayOnly || f.InboundLeg.IsDirect()));
        }

        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.AsParallel().Where(Filter(context));

        public IObservable<Flight> ProcessFlights(IObservable<Flight> flights, AvailabilitySearch context) => flights.Where(Filter(context));
    }

    #endregion

    #region RemoveFlightsWithHashModulo

    public class RemoveFlightsModulo1 : IFlightProcessor
    {
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.Where(f => f.Id % context.Mod1 == 0);
    }

    public class RemoveFlightsModulo2 : IFlightProcessor
    {
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.Where(f => f.Id % context.Mod2 == 0);
    }

    public class RemoveFlightsModulo1Parallel : IFlightProcessor
    {
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.AsParallel().Where(f => f.Id % context.Mod1 == 0);
    }

    public class RemoveFlightsModulo2Parallel : IFlightProcessor
    {
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.AsParallel().Where(f => f.Id % context.Mod2 == 0);
    }

    public class RemoveFlightsModulo1Lambda : IFlightProcessor, IObservableFlightProcessor
    {
        public static Func<Flight, bool> Filter(AvailabilitySearch context) => flight => flight.Id % context.Mod1 == 0;
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.AsParallel().Where(Filter(context));
        public IObservable<Flight> ProcessFlights(IObservable<Flight> flights, AvailabilitySearch context) => flights.Where(Filter(context));
    }

    public class RemoveFlightsModulo2Lambda : IFlightProcessor, IObservableFlightProcessor
    {
        public static Func<Flight, bool> Filter(AvailabilitySearch context) => flight => flight.Id % context.Mod2 == 0;
        public IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context) => flights.AsParallel().Where(Filter(context));
        public IObservable<Flight> ProcessFlights(IObservable<Flight> flights, AvailabilitySearch context) => flights.Where(Filter(context));
    }

    #endregion

    public class FlightObserver : IObserver<Flight>
    {
        public string Prefix { get; }
        public FlightObserver(string prefix) { this.Prefix = prefix; }
        void IObserver<Flight>.OnNext(Flight flight) { Console.WriteLine($"{Prefix} Flight {flight.Name}"); }
        void IObserver<Flight>.OnError(Exception error) { Console.WriteLine($"Error {error.Message}"); }
        void IObserver<Flight>.OnCompleted() { Console.WriteLine("Done"); }
    }

    public class FlightCountObserver : IObserver<Flight>
    {
        public List<Flight> Result { get; } = new List<Flight>();
        public FlightCountObserver() { }
        void IObserver<Flight>.OnNext(Flight flight) { this.Result.Add(flight); }
        void IObserver<Flight>.OnError(Exception error) { }
        void IObserver<Flight>.OnCompleted() { }
    }

    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class Bench
    {
        private const int flightsPerJob = 300_000;
        private IEnumerable<Flight> flightsEnumerable;
        private AvailabilitySearch search;

        [GlobalSetup]
        public void Setup()
        {
            this.search = new AvailabilitySearch { OneWayOnly = false, IsDirectFlight = true, Mod1 = 2, Mod2 = 3 };
            this.flightsEnumerable = DemoData.CreateRandomResult(flightsPerJob);
        }

        private int RunSequentially(params IFlightProcessor[] processors)
        {
            IEnumerable<Flight> result = flightsEnumerable;
            foreach (var p in processors)
            {
                result = p.DoFlightProcessing(result, search);
            }
            return result.Count();
        }

        private int RunReactive(params IObservableFlightProcessor[] processors)
        {
            // IObservable<Flight> flightsObservable = flightsEnumerable.ToObservable();
            var observer = new FlightCountObserver();

            var s = new Subject<Flight>();
            s.Where(f =>
                {
                    var allowIndirectFlights = !search.IsDirectFlight;
                    var oneWayOnly = search.OneWayOnly;
                    return allowIndirectFlights || (f.OutboundLeg.IsDirect() && (oneWayOnly || f.InboundLeg.IsDirect()));
                })
                .Where(flight => flight.Id % search.Mod1 == 0)
                .Where(flight => flight.Id % search.Mod2 == 0)
                .Subscribe(observer);

            foreach (var f in flightsEnumerable)
            {
                s.OnNext(f);
            }
            s.OnCompleted();

            return observer.Result.Count();
        }

        [Benchmark]
        public int Original() => RunSequentially(
            new RemoveNonDirectFlightsOriginal(),
            new RemoveFlightsModulo1(),
            new RemoveFlightsModulo2());

        [Benchmark]
        public int Parallel() => RunSequentially(
            new RemoveNonDirectFlightsParallel(),
            new RemoveFlightsModulo1Parallel(),
            new RemoveFlightsModulo2Parallel());

        [Benchmark]
        public int Lambda() => RunSequentially(
            new RemoveNonDirectFlightsLambda(),
            new RemoveFlightsModulo1Lambda(),
            new RemoveFlightsModulo2Lambda());

        [Benchmark]
        public int Reactive() => RunReactive(
            new RemoveNonDirectFlightsLambda(),
            new RemoveFlightsModulo1Lambda(),
            new RemoveFlightsModulo2Lambda());

        //static void Foo()
        //{
        //    static string compare(TimeSpan baseline, TimeSpan comparison) =>
        //        (baseline.TotalMilliseconds, comparison.TotalMilliseconds) switch
        //        {
        //            var (b, c) when b > c => $"{(b / c).ToString("F1")} faster",
        //            var (b, c) => $"{(c / b).ToString("F1")} slower"
        //       };
        //    Console.Out.WriteLine($"Serial:   {timeSerial  }ms -> 100%");
        //    Console.Out.WriteLine($"Parallel  {timeParallel}ms -> {compare(timeSerial, timeParallel)}");
        //    Console.Out.WriteLine($"Lambda    {timeLambda  }ms -> {compare(timeSerial, timeLambda)}");
        //    Console.Out.WriteLine($"Observer  {timeObserver}ms -> {compare(timeSerial, timeObserver)}");
        //    var resultsLookConsistent = serialCount == parallelCount && serialCount == lambdaCount && serialCount == observerCount;
        //    if (resultsLookConsistent)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Green;
        //        Console.Out.WriteLine($"Consistent results");
        //    }
        //    else
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.Out.WriteLine($"Inconsistent results: {serialCount} {parallelCount} {lambdaCount} {observerCount}");
        //    }
        //    Console.ResetColor();
        //}
    }

    internal class Program
    {
        private static async Task Main()
        {
            var summary = BenchmarkRunner.Run<Bench>();
            // await RedisPlay();
            await Console.In.ReadLineAsync();
        }

        private static async Task RedisPlay()
        {
            using var redis = ConnectionMultiplexer.Connect("localhost");
            int flightsPerBatch = 30_000;
            var channelName = "foo";

            var search = new AvailabilitySearch { OneWayOnly = false, IsDirectFlight = true };

            static Flight[] deserialize(RedisValue redisValue)
            {
                var bytes = (byte[])redisValue;
                var json = Encoding.UTF8.GetString(bytes);
                return json.DeserializeJSON<Flight[]>();
            }

            // Create an observer to listen on a Redis channel
            var observable = new RedisObservable(multiplexer: redis, channelName: channelName);
            var flightObservable = observable
                .Select(deserialize)
                .SelectMany(s => s)
                .Where(RemoveNonDirectFlightsLambda.Filter(search));

            flightObservable.Subscribe(new FlightObserver("1"));

            await Console.Out.WriteLineAsync("Listening... ");
            await Console.In.ReadLineAsync();

            var data = new DemoData(redis, channelName);
            var cts = new CancellationTokenSource();
            var publishTask = data.PublishEventsAsync(cts.Token, flightsPerBatch: flightsPerBatch);

            await Console.Out.WriteLineAsync("Pushing... ");
            await Console.In.ReadLineAsync();
            cts.Cancel();
            await publishTask;
            await Console.Out.WriteLineAsync("Done... ");
        }
    }

    public class RedisObservable : ObservableBase<RedisValue>, IDisposable
    {
        private readonly ConnectionMultiplexer _connection;
        private readonly ISubscriber _subscription;
        private readonly List<IObserver<RedisValue>> _observerList = new List<IObserver<RedisValue>>();

        protected internal RedisObservable(ConnectionMultiplexer multiplexer, string channelName)
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

    public class EnumerableObservable<T> : IObservable<T>, IDisposable
    {
        private readonly IEnumerable<T> enumerable;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly CancellationToken cancellationToken;
        private readonly Task workerTask;
        private readonly List<IObserver<T>> observerList = new List<IObserver<T>>();

        public EnumerableObservable(IEnumerable<T> enumerable, CancellationToken ct)
        {
            this.enumerable = enumerable;
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            this.cancellationToken = cancellationTokenSource.Token;
            this.workerTask = Task.Factory.StartNew(() =>
            {
                foreach (var value in this.enumerable)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var observer in observerList)
                        observer.OnNext(value);
                }
            }, this.cancellationToken);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observerList.Add(observer);
            return null;
        }

        public void Dispose()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                while (!workerTask.IsCanceled) { Thread.Sleep(100); }
            }
            cancellationTokenSource.Dispose();
            workerTask.Dispose();
            foreach (var observer in observerList)
            {
                observer.OnCompleted();
            }
        }
    }

    public static class MyUtils
    {
        public static void Dump<T>(this IObservable<T> source, string name)
        {
            source.Subscribe(
                t => Console.WriteLine($"{name} --> {t}"),
                ex => Console.WriteLine($"{name} failed-->{ex.Message}"),
                () => Console.WriteLine($"{name} completed")
            );
        }

        public static bool IsDirect(this Leg l) => l != null && l.Segments != null && l.Segments.Count == 1 && l.Segments[0].AdditionalStops == 0;

        public static async Task<long> PublishAsync<T>(this IDatabase database, RedisChannel channel, T value)
        {
            var ms = new MemoryStream(value.AsJSON().ToUTF8Bytes());
            var redisValue = RedisValue.CreateFrom(ms);
            return await database.PublishAsync(channel, redisValue, CommandFlags.PreferMaster);
        }
    }

    internal class DemoData
    {
        public ConnectionMultiplexer Multiplexer { get; }
        public string ChannelName { get; }

        public DemoData(ConnectionMultiplexer multiplexer, string channelName)
        {
            this.Multiplexer = multiplexer;
            this.ChannelName = channelName;
        }

        public async Task PublishEventsAsync(CancellationToken ct, int flightsPerBatch)
        {
            var database = this.Multiplexer.GetDatabase();
            var channel = new RedisChannel(value: this.ChannelName, mode: RedisChannel.PatternMode.Literal);
            var i = 0;
            Flight n() => PossibleFlights[random.Next(0, PossibleFlights.Length - 1)](i++);

            while (true)
            {
                if (ct != null && ct.IsCancellationRequested) { break; }
                var flights = Enumerable.Range(0, flightsPerBatch).Select(_ => n()).ToArray();
                await database.PublishAsync(channel: channel, value: flights);
                // await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        private static readonly Segment DirectSegment = new Segment { AdditionalStops = 0 };
        private static readonly Segment NonDirectSegment = new Segment { AdditionalStops = 1 };
        private static readonly Leg DirectLeg = new Leg { Segments = new List<Segment> { DirectSegment } };
        private static readonly Leg NonDirectLeg = new Leg { Segments = new List<Segment> { NonDirectSegment } };

        private static Func<int, Flight> F(Leg flight, Leg returnFlight = null)
        {
            Flight create(int id) => new Flight { Name = $"flight {id}", Id = id, OutboundLeg = flight, InboundLeg = returnFlight };
            return create;
        }

        private static readonly Func<int, Flight>[] PossibleFlights = new[] { F(DirectLeg), F(NonDirectLeg), F(DirectLeg, DirectLeg), F(DirectLeg, NonDirectLeg), F(NonDirectLeg, DirectLeg), F(NonDirectLeg, NonDirectLeg) };
        private static readonly Random random = new Random();

        private static Func<int, Flight> CreateRandomFlight() => PossibleFlights[random.Next(0, PossibleFlights.Length - 1)];
        internal static IEnumerable<Flight> CreateRandomResult(int count) => Enumerable.Range(0, count).Select(i => CreateRandomFlight()(i)).ToArray();
    }

    #region cruft

    public interface IObservableFlightProcessor
    {
        IObservable<Flight> ProcessFlights(IObservable<Flight> flights, AvailabilitySearch context);
    }

    public interface IFlightProcessor
    {
        IEnumerable<Flight> DoFlightProcessing(IEnumerable<Flight> flights, AvailabilitySearch context);
    }

    public class Segment
    {
        public int AdditionalStops { get; set; }
    }

    public class Leg
    {
        public IList<Segment> Segments { get; set; }
    }

    public class Flight
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public Leg InboundLeg { get; set; }
        public Leg OutboundLeg { get; set; }
    }

    public class AvailabilitySearch
    {
        public bool IsDirectFlight { get; set; }
        public bool OneWayOnly { get; set; }
        public int Mod1 { get; set; }
        public int Mod2 { get; set; }
    }

    #endregion
}
