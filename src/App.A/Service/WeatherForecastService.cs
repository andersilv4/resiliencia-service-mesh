using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace App.A.Service
{
    public interface IWeatherForecastService
    {
        Task<IEnumerable<WeatherForecast>> Get();
    }

    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly HttpClient _httpClient;

        public WeatherForecastService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<WeatherForecast>>("https://localhost:5003/WeatherForecast");

            return result.Select(celsius => new WeatherForecast()
            {
                Date = celsius.Date,
                Temperature = 32 + (int)(celsius.Temperature / 0.5556),
                Summary = celsius.Summary
            });
        }
    }
}