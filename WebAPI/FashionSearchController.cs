namespace WebAPI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.FSharp.Core;
    using static Fashion.BusinessData;
    using static Fashion.Domain;
    using static Fundamentals.Types;

    [ApiController]
    [Route("[controller]")]
    public class FashionSearchController : ControllerBase
    {
        private readonly Func<BusinessData<FashionBusinessData>> getBusinessData;
        private readonly Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest;
        private readonly IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump;
        private readonly Func<PipelineSteps<FashionProcessingContext, FashionItem>> createPipelineSteps;
        private readonly IDistributedSearchConfiguration demoCredential;

        public FashionSearchController(
            IDistributedSearchConfiguration demoCredential,
            IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump,
            Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest,
            Func<PipelineSteps<FashionProcessingContext, FashionItem>> createPipelineSteps,
            Func<BusinessData<FashionBusinessData>> getBusinessData)
        {
            (this.providerResponsePump, this.sendProviderSearchRequest, this.createPipelineSteps, this.getBusinessData) =
                (providerResponsePump, sendProviderSearchRequest, createPipelineSteps, getBusinessData);
            this.demoCredential = demoCredential;
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
                this.SubtractExpectedComputeTime(
                    TimeSpan.FromMilliseconds(timeout)));

            var providerSearchRequest = new ProviderSearchRequest<FashionSearchRequest>(
                requestID: this.CreateRequestID(),
                responseTopic: Startup.GetCurrentComputeNodeResponseTopic(this.demoCredential),
                searchRequest: searchRequest);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var pipelineSteps = this.createPipelineSteps();
            var processingContext = new FashionProcessingContext(searchRequest, businessData.Data);

            // *** Subscribe and start processing inbound stream
            IObservable<FashionItem> responses =
                this.GetResponses(requestId: providerSearchRequest.RequestID)
                    .ApplySteps(processingContext, pipelineSteps.StreamingSteps)
                    .TakeUntil(responseMustBeReadyBy);

            // *** Send search request
            await this.sendProviderSearchRequest(providerSearchRequest);

            // *** Aggregate all things we have when `responseMustBeReadyBy` fires
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

        private IObservable<FashionItem> GetResponses(string requestId)
        {
            return this.providerResponsePump
                .Where(t => FSharpOption<string>.get_IsSome(t.RequestID)
                    && requestId == t.RequestID.Value)
                .SelectMany(providerSearchResponse => providerSearchResponse.Payload.Response);
        }

        private string CreateRequestID() => Guid.NewGuid().ToString();

        private TimeSpan SubtractExpectedComputeTime(TimeSpan timeout) => timeout.Subtract(TimeSpan.FromMilliseconds(100));
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