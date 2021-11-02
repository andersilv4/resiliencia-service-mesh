using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.B.Service;
using Microsoft.AspNetCore.Mvc;

namespace App.B.Controllers
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
            var summaries = await _service.Get();

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                Temperature = rng.Next(-20, 55),
                Summary = summaries.ToList()[rng.Next(summaries.Count())]
            });
        }
    }
}
