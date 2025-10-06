using BackTester.Strategies;
using Engine.Charts;
using Engine.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BackTester.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public async Task<string> Get()
        {
            var chart = new Chart()
            {
                HistoryFrom = new DateTime(2017, 01, 01),
                DateFrom = new DateTime(2018, 01, 01),
                Strategy = new BtcSystemStrategy(1000),
                Symbol = new Symbol("BTC-USD")
            };

            var context = new Context(chart);

            var plots = await context.ExecuteAsync();

            return JsonConvert.SerializeObject(plots,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}