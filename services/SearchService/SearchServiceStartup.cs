namespace Mercury.Services.SearchService
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Mercury.BusinessDataPump;
    using Mercury.Credentials;
    using Mercury.Customer.Fashion;
    using Mercury.Interfaces;
    using Mercury.Messaging;
    using Mercury.Utils;
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

        public SearchServiceStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.demoCredential = new DemoCredential();
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

            var responseDataPump = this.CreateProviderResponsePump<FashionItem>(
                topicPartitionID: GetCurrentComputeNodeResponseTopic(this.demoCredential));
            services.AddSingleton(_ => responseDataPump);
            services.AddSingleton<IDistributedSearchConfiguration>(_ => this.demoCredential);

            services.AddSingleton(_ => this.SendProviderSearchRequest());
            services.AddSingleton(_ => CreatePipelineSteps());
            services.AddSingleton(_ => this.GetCurrentBusinessData<FashionBusinessData, FashionBusinessDataUpdate>(
                newFashionBusinessData, FashionExtensions.ApplyFashionUpdate));
        }

        internal static TopicPartitionID GetCurrentComputeNodeResponseTopic(IDistributedSearchConfiguration demoCredential)
            => new TopicPartitionID(
                topicName: demoCredential.EventHubTopicNameResponses, partitionId: 1);

        internal static Func<PipelineSteps<FashionProcessingContext, FashionItem>> CreatePipelineSteps() => () =>
        {
            var s1 = PipelineStep<FashionProcessingContext, FashionItem>.NewPredicate(
                FSharpExtensions.ToFSharpFunc<Tuple<FashionProcessingContext, FashionItem>, bool>(
                    t => t.Item1.Query.Size == t.Item2.Size));

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

        internal Func<ProviderSearchRequest<FashionSearchRequest>, Task> SendProviderSearchRequest()
        {
            var requestProducer = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>(
                demoCredential: this.demoCredential, partitionId: null);

            return searchRequest => requestProducer.SendMessage(searchRequest, requestId: searchRequest.RequestID);
        }

        internal IObservable<Message<ProviderSearchResponse<T>>> CreateProviderResponsePump<T>(TopicPartitionID topicPartitionID)
        {
            var blobContainerClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{this.demoCredential.StorageOffloadAccountName}.blob.core.windows.net/{this.demoCredential.StorageOffloadContainerNameResponses}/"),
                credential: this.demoCredential.AADServicePrincipal);

            var messagingClient = MessagingClients
                .WithStorageOffload<ProviderSearchResponse<T>>(
                    demoCredential: this.demoCredential,
                    topicPartitionID: topicPartitionID,
                    storageOffload: blobContainerClient.ToStorageOffload());

            var connectable = messagingClient
                .CreateObervable(SeekPosition.FromTail)
                .Publish();

            connectable.Connect();

            return connectable.AsObservable();
        }

        internal Func<BusinessData<TBusinessData>> GetCurrentBusinessData<TBusinessData, TBusinessDataUpdate>(
            Func<TBusinessData> createEmptyBusinessData,
            Func<TBusinessData, TBusinessDataUpdate, TBusinessData> applyUpdate)
        {
            var businessDataUpdates = new BusinessDataPump<TBusinessData, TBusinessDataUpdate>(
                demoCredential: this.demoCredential,
                createEmptyBusinessData: createEmptyBusinessData,
                applyUpdate: applyUpdate,
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{this.demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{this.demoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: this.demoCredential.AADServicePrincipal));

            businessDataUpdates.StartUpdateProcess().Wait();
            return () => businessDataUpdates.BusinessData;
        }
    }
}