namespace WebAPI
{
    using System;
    using System.Threading.Tasks;
    using BusinessDataAggregation;
    using Fashion.BusinessData;
    using Microsoft.AspNetCore.Mvc;
    using static Fundamentals.Types;

    [ApiController]
    [Route("[controller]")]
    public class BusinessDataUpdateController : ControllerBase
    {
        private readonly BusinessDataPump<BusinessData, BusinessDataUpdate> businessDataProvider;

        public BusinessDataUpdateController(BusinessDataPump<BusinessData, BusinessDataUpdate> businessDataProvider)
        {
            this.businessDataProvider = businessDataProvider;
        }

        [HttpPost]
        public async Task<(Offset, BusinessData)> Post(BusinessDataUpdate bdu)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return (Offset.NewOffset(-1), null);
        }

        [HttpGet]
        public async Task<BusinessData> Get(Offset offset)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            throw new NotSupportedException();
        }
    }
}