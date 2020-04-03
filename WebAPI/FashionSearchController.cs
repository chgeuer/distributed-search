namespace WebAPI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using DataTypesFSharp;
    using Fundamentals;
    using Interfaces;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class FashionSearchController : ControllerBase
    {
        private readonly Func<BusinessData> getBusinessData;
        private readonly Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest;
        private readonly IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump;
        private readonly Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>> createPipelineSteps;

        public FashionSearchController(
            IObservable<Message<ProviderSearchResponse<FashionItem>>> providerResponsePump,
            Func<ProviderSearchRequest<FashionSearchRequest>, Task> sendProviderSearchRequest,
            Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>> createPipelineSteps,
            Func<BusinessData> getBusinessData)
        {
            (this.providerResponsePump, this.sendProviderSearchRequest, this.createPipelineSteps, this.getBusinessData) =
                (providerResponsePump, sendProviderSearchRequest, createPipelineSteps, getBusinessData);
        }

        [HttpGet]
        public async Task<SearchResponse<FashionItem>> Get(int size, string type = "Hat", int timeout = 15000)
        {
            /* curl --silent "http://localhost:5000/fashionsearch?size=54&type=Throusers&timeout=15000" | jq
             * curl --silent "http://localhost:5000/fashionsearch?size=16&type=Hat&timeout=5000" | jq
             * curl --silent "http://localhost:5000/fashionsearch?size=15&type=Hat&timeout=5000" | jq
             **/

            // snap a copy of the business data early in the process
            var businessData = this.getBusinessData();

            var responseMustBeReadyBy = DateTimeOffset.Now.Add(
                this.SubtractExpectedComputeTime(
                    TimeSpan.FromMilliseconds(timeout)));

            var query = new FashionSearchRequest(size: size, fashionType: type);
            var searchRequest = new ProviderSearchRequest<FashionSearchRequest>(
                requestID: this.CreateRequestID(),
                responseTopic: Startup.GetCurrentComputeNodeResponseTopic(),
                query: query);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // *** Subscribe and start processing inbound stream
            IObservable<FashionItem> responses =
                this.GetResponses(requestId: searchRequest.RequestID)
                    .ApplySteps(new ProcessingContext(query, businessData), this.createPipelineSteps())
                    .TakeUntil(responseMustBeReadyBy);

            // *** Send search request
            await this.sendProviderSearchRequest(searchRequest);

            // *** Aggregate all things we have when `responseMustBeReadyBy` fires
            FashionItem[] items = responses
                .ToEnumerable()
                .ApplySteps(new ProcessingContext(query, businessData), this.createPipelineSteps())
                .ToArray(); // .OrderBy(GlobalOrder).Take(2000);

            stopwatch.Stop();
            return new SearchResponse<FashionItem>
            {
                RequestID = searchRequest.RequestID,
                Items = items,
                Timing = $"Duration: {stopwatch.Elapsed.TotalSeconds:N3}",
                Version = businessData.Version,
                BusinessData = businessData,
            };
        }

        private IObservable<FashionItem> GetResponses(string requestId) =>
            this.providerResponsePump
                .Where(t => ((string)t.Properties["requestIDString"]) == requestId)
                .SelectMany(searchResponse => searchResponse.Value.Response);

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