namespace WebAPI
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BusinessDataAggregation;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Messaging;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static BusinessLogic.Logic;
    using static Fundamentals.Types;

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

        internal static Func<PipelineSteps<ProcessingContext, FashionItem>> CreatePipelineSteps() => () =>
        {
            var s1 = PipelineStep<ProcessingContext, FashionItem>.NewPredicate(
                FSharpExtensions.ToFSharpFunc<Tuple<ProcessingContext, FashionItem>, bool>(
                    t => t.Item1.Query.Size == t.Item2.Size));

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

        internal static Func<ProviderSearchRequest<FashionSearchRequest>, Task> SendSearchRequest()
        {
            var requestProducer = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>(partitionId: null);

            return searchRequest => requestProducer.SendMessage(searchRequest, requestId: searchRequest.RequestID);
        }

        internal static ResponseTopicAddress GetCurrentComputeNodeResponseTopic()
            => new ResponseTopicAddress(topicName: DemoCredential.EventHubTopicNameResponses, partitionId: 1);

        internal static IObservable<Message<ProviderSearchResponse<T>>> CreateProviderResponsePump<T>()
        {
            var messagingClient = MessagingClients
                .WithStorageOffload<ProviderSearchResponse<T>>(
                    responseTopicAddress: GetCurrentComputeNodeResponseTopic(),
                    accountName: DemoCredential.StorageOffloadAccountName,
                    containerName: DemoCredential.StorageOffloadContainerNameResponses);

            /* var replaySubject = new ReplaySubject<EventData>(window: TimeSpan.FromSeconds(15));
             */
            var connectable =
                messagingClient
                .CreateObervable(SeekPosition.FromTail)
                .Do(onNext: m => Console.WriteLine($"XXXXXXXX {m.Offset}"))
                .Publish();
                /* .Multicast(replaySubject);*/

            connectable.Connect();

            return connectable // replaySubject
                .AsObservable();
        }

        internal static Func<BusinessData> CreateBusinessData()
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