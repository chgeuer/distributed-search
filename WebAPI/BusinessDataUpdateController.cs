namespace WebAPI
{
    using System;
    using System.Threading.Tasks;
    using BusinessDataAggregation;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class BusinessDataUpdateController : ControllerBase
    {
        private readonly BusinessDataProvider businessDataProvider;

        public BusinessDataUpdateController(BusinessDataProvider businessDataProvider)
        {
            this.businessDataProvider = businessDataProvider;
        }

        [HttpPost]
        public async Task Post()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}