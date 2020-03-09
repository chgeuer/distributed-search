namespace WebAPI
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
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
    using Microsoft.FSharp.Collections;

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton(_ => CreateEventHubObservable());
            services.AddSingleton(_ => GetBusinessData());
            services.AddSingleton(_ => SendSearchRequest());
            services.AddSingleton(_ => CreateBusinessSteps());
        }

        private static Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>> CreateBusinessSteps() => () =>
        {
            return new IBusinessLogicStep<ProcessingContext, FashionItem>[]
            {
                    // new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.Size == i.Size),
                    // new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.FashionType == i.FashionType),
                    new SizeFilter(),

                    // GenericBetterAlternativeFilter<ProcessingContext, FashionItem>.FilterCheaperPrice(fi => fi.StockKeepingUnitID, fi => fi.Price),
                    new FashionTypeFilter(),
                    new MarkupAdder(),
            };
        };

        private static Func<BusinessData> GetBusinessData() => () =>
        {
            return new BusinessData(
              markup: new FSharpMap<string, decimal>(new[]
              {
                    Tuple.Create(FashionTypes.Hat, 0_12m),
                    Tuple.Create(FashionTypes.Throusers, 1_50m),
              }),
              defaultMarkup: 1_00);
        };

        private static string GetCurrentComputeNodeResponseTopic() => DemoCredential.EventHubTopicNameResponses;

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
                eventHubName: GetCurrentComputeNodeResponseTopic(),
                credential: DemoCredential.AADServicePrincipal);

            /* var replaySubject = new ReplaySubject<EventData>(window: TimeSpan.FromSeconds(15));
             */

            var connectable = client
                .CreateObservable()
                .Select(partitionEvent => partitionEvent.Data)
                .Publish();
            /* .Multicast(replaySubject);*/

            connectable.Connect();

            return connectable // replaySubject
                .AsObservable();
        }
    }
}