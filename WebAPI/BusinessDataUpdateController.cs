namespace WebAPI
{
    using System;
    using System.Threading.Tasks;
    using BusinessDataAggregation;
    using Microsoft.AspNetCore.Mvc;
    using static Fashion.BusinessData;
    using static Fundamentals.Types;

    [ApiController]
    [Route("[controller]")]
    public class BusinessDataUpdateController : ControllerBase
    {
        private readonly BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate> businessDataProvider;

        public BusinessDataUpdateController(BusinessDataPump<FashionBusinessData, FashionBusinessDataUpdate> businessDataProvider)
        {
            this.businessDataProvider = businessDataProvider;
        }

        [HttpPost]
        public async Task<(Offset, FashionBusinessData)> Post(FashionBusinessDataUpdate bdu)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return (Offset.NewOffset(-1), null);
        }

        [HttpGet]
        public async Task<FashionBusinessData> Get(Offset offset)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            throw new NotSupportedException();
        }
    }
}