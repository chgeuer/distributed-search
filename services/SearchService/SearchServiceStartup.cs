﻿namespace Mercury.Services.SearchService;

using Azure.Storage.Blobs;
using Mercury.BusinessDataPump;
using Mercury.Credentials;
using Mercury.Customer.Fashion;
using Mercury.Interfaces;
using Mercury.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Fundamentals.Types;
using static Mercury.Customer.Fashion.BusinessData;
using static Mercury.Customer.Fashion.Domain;
using static Mercury.Fundamentals.BusinessData;

public class SearchServiceStartup
{
    public IConfiguration Configuration { get; }

    private readonly IDistributedSearchConfiguration demoCredential;
    private readonly BlobContainerClient snapshotContainerClient;
    private readonly TopicAndPartition responseTopicAndPartition;

    public SearchServiceStartup(IConfiguration configuration)
    {
        this.Configuration = configuration;
        this.demoCredential = new DemoCredential();

        var computeNodeId = 100;

        this.responseTopicAndPartition = new TopicAndPartition(
            topicName: this.demoCredential.EventHubTopicNameResponses,
            partitionSpecification: PartitionSpecification.NewComputeNodeID(computeNodeId));

        this.snapshotContainerClient = new BlobContainerClient(
            blobContainerUri: new Uri($"https://{this.demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{this.demoCredential.BusinessDataSnapshotContainerName}/"),
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
        services.AddSingleton(_ => this.SendProviderSearchRequest());
        services.AddSingleton(_ => this.CreateProviderResponsePump<FashionItem>(this.responseTopicAndPartition));
        services.AddSingleton<IDistributedSearchConfiguration>(_ => this.demoCredential);

        services.AddSingleton(_ => CreatePipelineSteps());

        services.AddSingleton(_ => this.GetCurrentBusinessData<FashionBusinessData, FashionBusinessDataUpdate>(
           newFashionBusinessData, FashionBusinessDataExtensions.ApplyFashionUpdate));
    }

    private static Func<PipelineSteps<FashionBusinessData, FashionSearchRequest, FashionItem>> CreatePipelineSteps() => () =>
    {
        // Func<FashionBusinessData, FashionSearchRequest, FashionItem, bool> p = (bd, sr, i) => sr.Size == i.Size;
        // var s1 = PipelineStep<FashionBusinessData, FashionSearchRequest, FashionItem>.NewPredicate(p.ToFSharpFunc());
        return new PipelineSteps<FashionBusinessData, FashionSearchRequest, FashionItem>
        {
            StreamingSteps = new IBusinessLogicStep<FashionBusinessData, FashionSearchRequest, FashionItem>[]
            {
                // new StatefulMixAndMatchFilter(),
                // new GenericFilter<FashionBusinessData, FashionSearchRequest, FashionItem>((bd, sr, i) => sr.Size == i.Size),
                // new GenericFilter<FashionBusinessData, FashionSearchRequest, FashionItem>((bd, sr, i) => sr.FashionType == i.FashionType),
                new SizeFilter(),

                // GenericBetterAlternativeFilter<FashionBusinessData, FashionSearchRequest, FashionItem>.FilterCheaperPrice(fi => fi.StockKeepingUnitID, fi => fi.Price),
                new FashionTypeFilter(),
                new MarkupAdder(),
            },
            FinalSteps = new IBusinessLogicStep<FashionBusinessData, FashionSearchRequest, FashionItem>[]
            {
                new OrderByPriceFilter(),
            },
        };
    };

    /// <summary>
    /// A function which can asyncronously send out a provider search request.
    /// </summary>
    /// <returns>Returns a function which can asyncronously send out a provider search request.</returns>
    private Func<ProviderSearchRequest<FashionSearchRequest>, Task> SendProviderSearchRequest()
    {
        var requestProducer = MessagingClients.Requests<ProviderSearchRequest<FashionSearchRequest>>(this.demoCredential);

        return searchRequest => requestProducer.SendRequestResponseMessage(searchRequest, requestId: searchRequest.RequestID);
    }

    private IObservable<RequestResponseMessage<ProviderSearchResponse<T>>> CreateProviderResponsePump<T>(TopicAndPartition topicAndPartition)
    {
        var messagingClient = MessagingClients
            .Responses<ProviderSearchResponse<T>>(
                demoCredential: this.demoCredential,
                topicAndPartition: topicAndPartition);

        var connectable = messagingClient
            .CreateObervable()
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

    private Func<TopicAndPartition> GetTopicAndComputeNodeID() => () =>
    {
        return this.responseTopicAndPartition;
    };
}