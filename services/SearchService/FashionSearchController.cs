namespace Mercury.Services.SearchService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Mercury.Interfaces;
    using Mercury.Utils.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using static Fundamentals.Types;
    using static Mercury.Customer.Fashion.BusinessData;
    using static Mercury.Customer.Fashion.Domain;

    [ApiController]
    [Route("[controller]")]
    public class FashionSearchController : ControllerBase
    {
        private readonly IDistributedSearchConfiguration searchConfiguration;
        private readonly Func<BusinessData<FashionBusinessData>> getBusinessData;
        private readonly Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest;
        private readonly IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump;
        private readonly Func<PipelineSteps<FashionProcessingContext, FashionItem>> createPipelineSteps;
        private readonly Func<TopicPartitionID> getTopicPartitionID;

        /// <summary>
        /// Initializes a new instance of the <see cref="FashionSearchController"/> class.
        /// </summary>
        /// <param name="searchConfiguration">The configuration.</param>
        /// <param name="providerResponsePump">Pumps provider search responses into the compute node.</param>
        /// <param name="sendProviderSearchRequest">Sends a provider search request to the requests topic.</param>
        /// <param name="createPipelineSteps">Creates the processing pipeline steps.</param>
        /// <param name="getBusinessData">Returns the most recent business data.</param>
        /// <param name="getTopicPartitionID">Returns the <see cref="TopicPartitionID"/>.</param>
        public FashionSearchController(
            IDistributedSearchConfiguration searchConfiguration,
            IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump,
            Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest,
            Func<PipelineSteps<FashionProcessingContext, FashionItem>> createPipelineSteps,
            Func<BusinessData<FashionBusinessData>> getBusinessData,
            Func<TopicPartitionID> getTopicPartitionID)
        {
            (this.providerResponsePump, this.sendProviderSearchRequest, this.createPipelineSteps, this.getBusinessData, this.searchConfiguration, this.getTopicPartitionID) =
                (providerResponsePump, sendProviderSearchRequest, createPipelineSteps, getBusinessData, searchConfiguration, getTopicPartitionID);
        }

        [HttpGet]
        public async Task<SearchResponse<FashionBusinessData, FashionItem>> Get(int size, string type = "Hat", int timeout = 15000)
        {
            var searchRequest = new FashionSearchRequest(size: size, fashionType: type);

            /* curl --silent "http://localhost:5000/fashionsearch?size=54&type=Throusers&timeout=15000" | jq
             * curl --silent "http://localhost:5000/fashionsearch?size=16&type=Hat&timeout=5000" | jq
             * curl --silent "http://localhost:5000/fashionsearch?size=15&type=Hat&timeout=5000" | jq
             **/

            // snap a copy of the business data early in the process
            var businessData = this.getBusinessData();

            var responseMustBeReadyBy = DateTimeOffset.Now.Add(
                TimeSpan.FromMilliseconds(timeout));

            var providerSearchRequest = new ProviderSearchRequest<FashionSearchRequest>(
                requestID: Guid.NewGuid().ToString(),
                responseTopic: this.getTopicPartitionID(),
                searchRequest: searchRequest);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var pipelineSteps = this.createPipelineSteps();
            var processingContext = new FashionProcessingContext(searchRequest, businessData.Data);

            // Subscribe and start processing inbound stream. This is a non-blocking call.
            IObservable<FashionItem> responses =
                this.providerResponsePump
                    .Where(t => t.RequestID.OptionEqualsValue(t.RequestID.Value))
                    .SelectMany(providerSearchResponse => providerSearchResponse.Payload.Response)
                    .ApplySteps(processingContext, pipelineSteps.StreamingSteps)
                    .TakeUntil(responseMustBeReadyBy);

            // Send search provider request into requests topic.
            await this.sendProviderSearchRequest(providerSearchRequest);

            // Convert IObservable<> into IEnumerable, apply the sequential steps, and
            // materialize everything into an array.
            // Aggregate all things we have, by when `responseMustBeReadyBy` fires
            FashionItem[] items = responses
                .ToEnumerable()
                .ApplySteps(processingContext, pipelineSteps.FinalSteps)
                .ToArray();

            stopwatch.Stop();
            return new SearchResponse<FashionBusinessData, FashionItem>
            {
                RequestID = providerSearchRequest.RequestID,
                Items = items,
                Timing = $"Duration: {stopwatch.Elapsed.TotalSeconds:N3}",
                Version = businessData.Offset.Item,
                BusinessData = businessData.Data,
            };
        }
    }

    public class SearchResponse<TBusinessData, TItem>
    {
        public string RequestID { get; set; }

        public string Timing { get; set; }

        public long Version { get; set; }

        public IEnumerable<TItem> Items { get; set; }

        public TBusinessData BusinessData { get; set; }
    }
}