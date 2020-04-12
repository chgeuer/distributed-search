namespace Mercury.Services.SearchService
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Mercury.BusinessDataPump;
    using Mercury.Credentials;
    using Mercury.Customer.Fashion;
    using Mercury.Fundamentals;
    using Mercury.Interfaces;
    using Mercury.Messaging;
    using Mercury.Utils.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Fundamentals.Types;
    using static Mercury.Customer.Fashion.BusinessData;
    using static Mercury.Customer.Fashion.Domain;
    using static Mercury.Fundamentals.BusinessData;
    using static Mercury.Fundamentals.BusinessLogic;

    public class SearchServiceStartup
    {
        public IConfiguration Configuration { get; }

        private readonly IDistributedSearchConfiguration demoCredential;
        private readonly BlobContainerClient snapshotContainerClient;
        private readonly BlobContainerClient storageOffloadStorage;
        private readonly TopicAndComputeNodeID topicAndComputeNodeID;

        public SearchServiceStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.demoCredential = new DemoCredential();

            var computeNodeId = 100;

            this.topicAndComputeNodeID = new TopicAndComputeNodeID(
                topicName: this.demoCredential.EventHubTopicNameResponses,
                computeNodeId: computeNodeId);

            this.snapshotContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{this.demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{this.demoCredential.BusinessDataSnapshotContainerName}/"),
                credential: this.demoCredential.AADServicePrincipal);

            this.storageOffloadStorage = new BlobContainerClient(
               blobContainerUri: new Uri($"https://{this.demoCredential.StorageOffloadAccountName}.blob.core.windows.net/{this.demoCredential.StorageOffloadContainerNameResponses}/"),
               credential: this.demoCredential.AADServicePrincipal);
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

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton(_ => this.GetTopicAndComputeNodeID());
            services.AddSingleton(_ => this.CreateProviderResponsePump<FashionItem>(this.topicAndComputeNodeID));
            services.AddSingleton<IDistributedSearchConfiguration>(_ => this.demoCredential);

            services.AddSingleton(_ => this.SendProviderSearchRequest());
            services.AddSingleton(_ => CreatePipelineSteps());
            services.AddSingleton(_ => this.GetCurrentBusinessData<FashionBusinessData, FashionBusinessDataUpdate>(
                newFashionBusinessData, FashionExtensions.ApplyFashionUpdate));
        }

        private static Func<PipelineSteps<FashionProcessingContext, FashionItem>> CreatePipelineSteps() => () =>
        {
            Func<FashionProcessingContext, FashionItem, bool> p = (c, i) => c.Query.Size == i.Size;

            var s1 = PipelineStep<FashionProcessingContext, FashionItem>.NewPredicate(p.ToFSharpFunc());

            return new PipelineSteps<FashionProcessingContext, FashionItem>
            {
                StreamingSteps = new IBusinessLogicStep<FashionProcessingContext, FashionItem>[]
                {
                    // new StatefulMixAndMatchFilter(),
                    // new GenericFilter<FashionProcessingContext, FashionItem>((c, i) => c.Query.Size == i.Size),
                    // new GenericFilter<FashionProcessingContext, FashionItem>((c,i) => c.Query.FashionType == i.FashionType),
                    new SizeFilter(),

                    // GenericBetterAlternativeFilter<FashionProcessingContext, FashionItem>.FilterCheaperPrice(fi => fi.StockKeepingUnitID, fi => fi.Price),
                    new FashionTypeFilter(),
                    new MarkupAdder(),
                },
                FinalSteps = Array.Empty<IBusinessLogicStep<FashionProcessingContext, FashionItem>>(),
            };
        };

        /// <summary>
        /// A function which can asyncronously send out a provider search request.
        /// </summary>
        /// <returns>Returns a function which can asyncronously send out a provider search request.</returns>
        private Func<ProviderSearchRequest<FashionSearchRequest>, Task> SendProviderSearchRequest()
        {
            var requestProducer = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>(
                demoCredential: this.demoCredential, computeNodeId: null);

            return searchRequest => requestProducer.SendMessage(searchRequest, requestId: searchRequest.RequestID);
        }

        private IObservable<Message<ProviderSearchResponse<T>>> CreateProviderResponsePump<T>(TopicAndComputeNodeID topicAndComputeNodeID)
        {
            var messagingClient = MessagingClients
                .WithStorageOffload<ProviderSearchResponse<T>>(
                    demoCredential: this.demoCredential,
                    topicAndComputeNodeID: topicAndComputeNodeID,
                    storageOffload: this.storageOffloadStorage.ToStorageOffload());

            var connectable = messagingClient
                .CreateObervable(SeekPosition.FromTail)
                .Publish();

            connectable.Connect();

            return connectable.AsObservable();
        }

        private Func<BusinessData<TBusinessData>> GetCurrentBusinessData<TBusinessData, TBusinessDataUpdate>(
            Func<TBusinessData> createEmptyBusinessData,
            Func<TBusinessData, TBusinessDataUpdate, TBusinessData> applyUpdate)
        {
            var businessDataUpdates = new BusinessDataPump<TBusinessData, TBusinessDataUpdate>(
                demoCredential: this.demoCredential,
                createEmptyBusinessData: createEmptyBusinessData,
                applyUpdate: applyUpdate,
                snapshotContainerClient: this.snapshotContainerClient);

            businessDataUpdates.StartUpdateProcess().Wait();
            return () => businessDataUpdates.BusinessData;
        }

        private Func<TopicAndComputeNodeID> GetTopicAndComputeNodeID() => () =>
        {
            return this.topicAndComputeNodeID;
        };
    }
}