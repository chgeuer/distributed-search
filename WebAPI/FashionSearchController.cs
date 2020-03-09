namespace WebAPI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Storage.Blobs;
    using Credentials;
    using DataTypesFSharp;
    using Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using static LanguageExt.Prelude;

    [ApiController]
    [Route("[controller]")]
    public class FashionSearchController : ControllerBase
    {
        private readonly IObservable<EventData> responseObservable;
        private readonly Func<SearchRequest, Task> sendSearchRequest;
        private readonly Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>> createBusinessLogic;
        private readonly Func<BusinessData> getBusinessData;
        private readonly BlobContainerClient blobContainerResponsesClient;

        public FashionSearchController(
            IObservable<EventData> responseObservable,
            Func<SearchRequest, Task> sendSearchRequest,
            Func<IEnumerable<IBusinessLogicStep<ProcessingContext, FashionItem>>> createBusinessLogic,
            Func<BusinessData> getBusinessData)
        {
            (this.responseObservable, this.sendSearchRequest, this.createBusinessLogic, this.getBusinessData) =
                (responseObservable, sendSearchRequest, createBusinessLogic, getBusinessData);

            this.blobContainerResponsesClient = new BlobContainerClient(
                blobContainerUri: new Uri($"https://{DemoCredential.StorageOffloadAccountName}.blob.core.windows.net/{DemoCredential.StorageOffloadContainerNameResponses}/"),
                credential: DemoCredential.AADServicePrincipal);
        }

        [HttpGet]
        public async Task<SearchResponse<FashionItem>> Get(int size, string type = "Hat", int timeout = 15000)
        {
            // snap a copy of the business data early in the process
            var businessData = this.getBusinessData();

            /* curl --silent "http://localhost:5000/fashionsearch?size=54&type=Throusers&timeout=15000" | jq
             * curl --silent "http://localhost:5000/fashionsearch?size=16&type=Hat&timeout=5000" | jq
             **/

            var responseMustBeReadyBy = DateTimeOffset.Now.Add(
                this.SubtractExpectedComputeTime(
                    TimeSpan.FromMilliseconds(timeout)));

            var query = new FashionQuery(size: size, fashionType: type);
            var searchRequest = new SearchRequest(
                requestID: this.CreateRequestID(),
                responseTopic: this.GetResponseTopicNameForThisComputeNode(),
                query: query);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var responses =
                this.GetResponses(requestId: searchRequest.RequestID)
                    .ApplySteps(new ProcessingContext(query, businessData), this.createBusinessLogic())
                    .TakeUntil(responseMustBeReadyBy);

            await this.sendSearchRequest(searchRequest);
            FashionItem[] items = responses.ToEnumerable().ToArray();

            stopwatch.Stop();
            return new SearchResponse<FashionItem>
            {
                RequestID = searchRequest.RequestID,
                Items = items,
                Timing = $"Duration: {stopwatch.Elapsed.TotalSeconds.ToString("N3")}",
                Version = businessData.Version,
            };
        }

        private IObservable<FashionItem> GetResponses(string requestId) =>
            this.responseObservable
                .Where(eventData => eventData.GetProperty<string>("requestIDString") == Some(requestId))
                .Select(eventData => eventData.GetBodyAsUTF8())
                .Select(jsonStr => JsonConvert.DeserializeObject<SearchResponse>(jsonStr))
                .Select(async r =>
                {
                    var blobClient = this.blobContainerResponsesClient.GetBlobClient(blobName: r.ResponseBlob);
                    var result = await blobClient.DownloadAsync();
                    var payload = await result.Value.Content.ReadJSON<SearchResponsePayload>();
                    await Console.Out.WriteLineAsync($"Downloaded response {payload.Response.First().Description} from {r.ResponseBlob}");
                    return payload;
                })
                .SelectMany(searchResponse => searchResponse.Result.Response);

        private string GetResponseTopicNameForThisComputeNode() => DemoCredential.EventHubTopicNameResponses;

        private string CreateRequestID() => Guid.NewGuid().ToString();

        private TimeSpan SubtractExpectedComputeTime(TimeSpan timeout) => timeout.Subtract(TimeSpan.FromMilliseconds(100));
    }

    public class SearchResponse<T>
    {
        public string RequestID { get; set; }

        public IEnumerable<T> Items { get; set; }

        public string Timing { get; set; }

        public long Version { get; set; }
    }
}