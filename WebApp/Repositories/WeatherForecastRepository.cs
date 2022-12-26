namespace WebApp.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapr;
    using Dapr.Client;
    using Microsoft.Extensions.Logging;

    public interface IDaprRepository<T>
    {
        Task Save(IEnumerable<T> weatherForecasts);

        Task<Dictionary<string, T>> Get(IEnumerable<string> ids);
    }

    public interface IWeatherForecastRepository : IDaprRepository<WeatherForecast>
    {
    }

    internal sealed class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<WeatherForecastRepository> _logger;

        public WeatherForecastRepository(DaprClient daprClient, ILogger<WeatherForecastRepository> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        public Task Save(IEnumerable<WeatherForecast> weatherForecasts)
        {
            var forecasts = weatherForecasts as WeatherForecast[] ?? weatherForecasts.ToArray();
            var tasks = new List<Task>(forecasts.Length);

            foreach (var weatherForecast in forecasts)
            {
                tasks.Add(_daprClient.SaveStateAsync(nameof(WeatherForecast), weatherForecast.Id,
                    weatherForecast));
            }

            foreach (var task in tasks)
            {
                try
                {
                    if (task.IsCompletedSuccessfully)
                        continue;

                    _ = SlowTask(task);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return Task.CompletedTask;

            async Task SlowTask(Task task)
            {
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "");
                    throw;
                }
            }
        }

        public Task<Dictionary<string, WeatherForecast>> Get(IEnumerable<string> ids)
        {
            var enumerable = ids as string[] ?? ids.ToArray();

            var promises =
                new Dictionary<string, Task<StateEntry<WeatherForecast>>>(enumerable.Length);

            foreach (var id in enumerable)
            {
                var stateEntry = _daprClient.GetStateEntryAsync<WeatherForecast>(nameof(WeatherForecast),
                    id);

                promises.Add(id, stateEntry);
            }

            var results = new Dictionary<string, WeatherForecast>(promises.Count);
            foreach (var promise in promises)
            {
                if (promise.Value.IsCompletedSuccessfully)
                {
                    results.Add(promise.Key, promise.Value.Result.Value);
                    continue;
                }

                results[promise.Key] = promise.Value.IsCompletedSuccessfully
                    ? promise.Value.Result.Value
                    : SlowTask(promise.Value).Result.Value;
            }

            return Task.FromResult(results);

            async Task<StateEntry<WeatherForecast>> SlowTask(Task<StateEntry<WeatherForecast>> task)
            {
                return await task;
            }
        }
    }
}