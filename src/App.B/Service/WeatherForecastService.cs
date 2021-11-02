using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace App.B.Service
{
    public interface IWeatherForecastService
    {
        Task<IEnumerable<string>> Get();
    }

    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly HttpClient _httpClient;

        public WeatherForecastService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<string>> Get()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<string>>("https://localhost:5005/WeatherForecast");
        }
    }
}