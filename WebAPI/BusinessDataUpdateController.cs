namespace WebAPI
{
    using System;
    using System.Threading.Tasks;
    using BusinessDataAggregation;
    using DataTypesFSharp;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class BusinessDataUpdateController : ControllerBase
    {
        private readonly BusinessDataPump businessDataProvider;

        public BusinessDataUpdateController(BusinessDataPump businessDataProvider)
        {
            this.businessDataProvider = businessDataProvider;
        }

        [HttpPost]
        public async Task<(Offset, BusinessData)> Post(BusinessDataUpdate bdu)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return (new Offset { OffsetValue = -1 }, null);
        }

        [HttpGet]
        public async Task<BusinessData> Get(Offset offset)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            throw new NotSupportedException();
        }
    }

    public class BusinessDataUpdate
    {
        public string Key { get; set; }

        public object Value { get; set; }
    }

    public class Offset
    {
        public long OffsetValue { get; set; }
    }
}