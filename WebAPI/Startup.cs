namespace WebAPI
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Messaging.AzureImpl;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

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
            services.AddSingleton(_ => SendSearchRequest());
            services.AddSingleton(_ => CreateBusinessSteps());
            services.AddSingleton(_ => CreateBusinessData());
        }

        private static Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>> CreateBusinessSteps() => () =>
        {
            return new IBusinessLogicStep<ProcessingContext, FashionItem>[]
            {
                    // new StatefulMixAndMatchFilter(),
                    // new GenericFilter<ProcessingContext, FashionItem>((c, i) => c.Query.Size == i.Size),
                    // new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.FashionType == i.FashionType),
                    new SizeFilter(),

                    // GenericBetterAlternativeFilter<ProcessingContext, FashionItem>.FilterCheaperPrice(fi => fi.StockKeepingUnitID, fi => fi.Price),
                    new FashionTypeFilter(),
                    new MarkupAdder(),
            };
        };

        private static string GetCurrentComputeNodeResponseTopic() => DemoCredential.EventHubTopicNameResponses;

        private static Func<SearchRequest, Task> SendSearchRequest()
        {
            var requestProducer = MessagingClients.Requests<SearchRequest>();

            return searchRequest => requestProducer.SendMessage(searchRequest, requestId: searchRequest.RequestID);
        }

        private static IObservable<Tuple<long, SearchResponse>> CreateEventHubObservable()
        {
            /* var replaySubject = new ReplaySubject<EventData>(window: TimeSpan.FromSeconds(15));
             */

            var connectable = MessagingClients
                .Responses<SearchResponse>(partitionId: "0")
                .CreateObervable(SeekPosition.Tail)
                .Publish();
                /* .Multicast(replaySubject);*/

            connectable.Connect();

            return connectable // replaySubject
                .AsObservable();
        }

        private static Func<BusinessData> CreateBusinessData()
        {
            var businessDataUpdates = new BusinessDataProvider(
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: DemoCredential.AADServicePrincipal));

            businessDataUpdates.StartUpdateProcess().Wait();
            return () => businessDataUpdates.BusinessData;
        }
    }
}