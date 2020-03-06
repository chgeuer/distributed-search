namespace WebAPI
{
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.Messaging.EventHubs.Producer;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<Func<SearchRequest, Task>>(_ => SendSearchRequest());
            services.AddSingleton<IObservable<EventData>>(_ => CreateEventHubObservable());
            services.AddSingleton<Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>>>(() =>
            {
                return new IBusinessLogicStep<ProcessingContext, FashionItem>[]
                {
                    // new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.Size == i.Size),
                    new SizeFilter(),
                    // new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.FashionType == i.FashionType),
                    new FashionTypeFilter(),
                    // GenericBetterAlternativeFilter<ProcessingContext, FashionItem>.FilterCheaperPrice(fi => fi.StockKeepingUnitID, fi => fi.Price),
                    new MarkupAdder(),
                };
            });
            services.AddSingleton<Func<BusinessData>>(() =>
                {
                    static decimal markup2(FashionItem fi) => fi.FashionType switch
                    {
                        var x when x == FashionTypes.Hat => 0_12,
                        var x when x == FashionTypes.Throusers => 1_50,
                        _ => 1_00,
                    };
                    return new BusinessData(markup: markup2);
                });
        }

        private static Func<SearchRequest, Task> SendSearchRequest()
        {
            var responseProducer = new EventHubProducerClient(
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: DemoCredential.EventHubTopicNameRequests,
                credential: DemoCredential.AADServicePrincipal);

            return searchRequest => responseProducer.SendJsonRequest(item: searchRequest, requestId: searchRequest.RequestID);
        }

        private static IObservable<EventData> CreateEventHubObservable()
        {
            var client = new EventHubConsumerClient(
                consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
                fullyQualifiedNamespace: $"{DemoCredential.EventHubName}.servicebus.windows.net",
                eventHubName: DemoCredential.EventHubTopicNameResponses,
                credential: DemoCredential.AADServicePrincipal);

            // var replaySubject = new ReplaySubject<EventData>(window: TimeSpan.FromSeconds(15));

            var connectable = client
                .CreateObservable()
                .Select(partitionEvent => partitionEvent.Data)
                .Publish();
            // .Multicast(replaySubject);

            connectable.Connect();

            return connectable // replaySubject
                .AsObservable();
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}