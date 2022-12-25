namespace WebApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapr.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Repositories;

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly DaprClient _daprClient;
        private readonly IWeatherForecastRepository _weatherForecastRepository;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, DaprClient daprClient,
            IWeatherForecastRepository weatherForecastRepository)
        {
            _logger = logger;
            _daprClient = daprClient;
            _weatherForecastRepository = weatherForecastRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var rng = new Random();

            var weatherForecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Id = index.ToString(),
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray();

            await _weatherForecastRepository.Save(weatherForecasts);

            var res = await _weatherForecastRepository.Get(weatherForecasts.Select(x => x.Id));

            return res.Values;
        }
    }
}