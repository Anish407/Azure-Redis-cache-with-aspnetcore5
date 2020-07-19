using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.HostedServices.EF;
using Azure.RedisCache.Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure.RedisCache.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, salesdbContext salesdbContext)
        {
            _logger = logger;
            SalesdbContext = salesdbContext;
        }

        public salesdbContext SalesdbContext { get; }

        [HttpGet]
        public async Task<IActionResult> Get([FromServices] IDistributedCache distributedCache)
        {
            try
            {
                var data = await distributedCache.GetStringAsync("sales");

                if (!string.IsNullOrEmpty(data)) return Ok(JsonConvert.DeserializeObject<IEnumerable<Sales>>(data));


                DistributedCacheEntryOptions cacheEntryOptions = new DistributedCacheEntryOptions();
                
                //invalidate or expire the cache after 0 days, 1 minutes and 30 seconds
                //absolute expiring will expire the cache after the specified time
                cacheEntryOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 1, 30));

                //use SetAbsoluteExpiration OR SetSlidingExpiration

                //sliding expiration will expire the cache if its not used for the specified time
                cacheEntryOptions.SetSlidingExpiration(new System.TimeSpan(0, 20, 0));

                //remove the cache
                await distributedCache.RemoveAsync("sales");

                var result = SalesdbContext.Sales.Take(20).ToList();
                await distributedCache.SetStringAsync("sales", JsonConvert.SerializeObject(result));
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
