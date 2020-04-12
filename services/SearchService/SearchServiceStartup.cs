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
    using static BusinessLogic.Logic;
    using static Fundamentals.Types;
    using static Mercury.Customer.Fashion.BusinessData;
    using static Mercury.Customer.Fashion.Domain;

    public class SearchServiceStartup
    {
        public IConfiguration Configuration { get; }

        private readonly IDistributedSearchConfiguration demoCredential;
        private readonly BlobContainerClient snapshotContainerClient;
        private readonly BlobContainerClient storageOffloadStorage;
        private readonly TopicPartitionID topicPartitionID;

        public SearchServiceStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.demoCredential = new DemoCredential();

            // TODO Currently this is locked to a partitionId 1
            this.topicPartitionID = new TopicPartitionID(topicName: this.demoCredential.EventHubTopicNameResponses, partitionId: 1);

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

            services.AddSingleton(_ => this.GetTopicPartitionID());
            services.AddSingleton(_ => this.CreateProviderResponsePump<FashionItem>(this.topicPartitionID));
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
                demoCredential: this.demoCredential, partitionId: null);

            return searchRequest => requestProducer.SendMessage(searchRequest, requestId: searchRequest.RequestID);
        }

        private IObservable<Message<ProviderSearchResponse<T>>> CreateProviderResponsePump<T>(TopicPartitionID topicPartitionID)
        {
            var messagingClient = MessagingClients
                .WithStorageOffload<ProviderSearchResponse<T>>(
                    demoCredential: this.demoCredential,
                    topicPartitionID: topicPartitionID,
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

        private Func<TopicPartitionID> GetTopicPartitionID() => () =>
        {
            return this.topicPartitionID;
        };
    }
}