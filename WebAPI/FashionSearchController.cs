namespace WebAPI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using DataTypesFSharp;
    using Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.FSharp.Core;
    using static Fundamentals.Types;

    [ApiController]
    [Route("[controller]")]
    public class FashionSearchController : ControllerBase
    {
        private readonly Func<BusinessData> getBusinessData;
        private readonly Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest;
        private readonly IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump;
        private readonly Func<PipelineSteps<ProcessingContext, FashionItem>> createPipelineSteps;

        public FashionSearchController(
            IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump,
            Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest,
            Func<PipelineSteps<ProcessingContext, FashionItem>> createPipelineSteps,
            Func<BusinessData> getBusinessData)
        {
            (this.providerResponsePump, this.sendProviderSearchRequest, this.createPipelineSteps, this.getBusinessData) =
                (providerResponsePump, sendProviderSearchRequest, createPipelineSteps, getBusinessData);
        }

        [HttpGet]
        public async Task<SearchResponse<FashionItem>> Get(int size, string type = "Hat", int timeout = 15000)
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
                responseTopic: Startup.GetCurrentComputeNodeResponseTopic(),
                searchRequest: searchRequest);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var pipelineSteps = this.createPipelineSteps();
            var processingContext = new ProcessingContext(searchRequest, businessData);

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
            return new SearchResponse<FashionItem>
            {
                RequestID = providerSearchRequest.RequestID,
                Items = items,
                Timing = $"Duration: {stopwatch.Elapsed.TotalSeconds:N3}",
                Version = businessData.Version.Item,
                BusinessData = businessData,
            };
        }

        private IObservable<FashionItem> GetResponses(string requestId) =>
            this.providerResponsePump
                .Where(t => t.RequestID == FSharpOption<string>.Some(requestId))
                .SelectMany(providerSearchResponse => providerSearchResponse.Payload.Response);

        private string CreateRequestID() => Guid.NewGuid().ToString();

        private TimeSpan SubtractExpectedComputeTime(TimeSpan timeout) => timeout.Subtract(TimeSpan.FromMilliseconds(100));
    }

    public class SearchResponse<T>
    {
        public string RequestID { get; set; }

        public IEnumerable<T> Items { get; set; }

        public string Timing { get; set; }

        public long Version { get; set; }

        public BusinessData BusinessData { get; set; }
    }
}