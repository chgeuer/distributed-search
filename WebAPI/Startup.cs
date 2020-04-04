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
    using Fundamentals;
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
            services.AddSingleton(_ => CreateProviderResponsePump<FashionItem>());
            services.AddSingleton(_ => SendSearchRequest());
            services.AddSingleton(_ => CreatePipelineSteps());
            services.AddSingleton(_ => CreateBusinessData());
        }

        internal static string GetCurrentComputeNodeResponseTopic() => DemoCredential.EventHubTopicNameResponses;

        private static Func<PipelineSteps<ProcessingContext, FashionItem>> CreatePipelineSteps() => () =>
        {
            return new PipelineSteps<ProcessingContext, FashionItem>
            {
                StreamingSteps = new IBusinessLogicStep<ProcessingContext, FashionItem>[]
                {
                    // new StatefulMixAndMatchFilter(),
                    // new GenericFilter<ProcessingContext, FashionItem>((c, i) => c.Query.Size == i.Size),
                    // new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.FashionType == i.FashionType),
                    new SizeFilter(),

                    // GenericBetterAlternativeFilter<ProcessingContext, FashionItem>.FilterCheaperPrice(fi => fi.StockKeepingUnitID, fi => fi.Price),
                    new FashionTypeFilter(),
                    new MarkupAdder(),
                },
                FinalSteps = Array.Empty<IBusinessLogicStep<ProcessingContext, FashionItem>>(),
            };
        };

        private static Func<ProviderSearchRequest<FashionSearchRequest>, Task> SendSearchRequest()
        {
            var requestProducer = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>(partitionId: null);

            return searchRequest => requestProducer.SendMessageWithRequestID(searchRequest, requestId: searchRequest.RequestID);
        }

        private static IObservable<Message<ProviderSearchResponse<T>>> CreateProviderResponsePump<T>()
        {
            /* var replaySubject = new ReplaySubject<EventData>(window: TimeSpan.FromSeconds(15));
             */

            var connectable = MessagingClients
                .WithStorageOffload<ProviderSearchResponse<T>>(
                    topicName: GetCurrentComputeNodeResponseTopic(),
                    partitionId: "0",
                    accountName: DemoCredential.StorageOffloadAccountName,
                    containerName: DemoCredential.StorageOffloadContainerNameResponses)
                .CreateObervable(SeekPosition.Tail)
                .Publish();
                /* .Multicast(replaySubject);*/

            connectable.Connect();

            return connectable // replaySubject
                .AsObservable();
        }

        private static Func<BusinessData> CreateBusinessData()
        {
            var businessDataUpdates = new BusinessDataPump(
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{DemoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{DemoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: DemoCredential.AADServicePrincipal));

            businessDataUpdates.StartUpdateProcess().Wait();
            return () => businessDataUpdates.BusinessData;
        }
    }
}