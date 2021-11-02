using System.Collections.Generic;
using System.Threading.Tasks;
using App.A.Service;
using Microsoft.AspNetCore.Mvc;

namespace App.A.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastService _service;

        public WeatherForecastController(IWeatherForecastService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            return await _service.Get();
        }
    }
}
