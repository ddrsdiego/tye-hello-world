﻿namespace WebApp.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapr.Client;
    using Microsoft.Extensions.Logging;

    public interface IDaprRepository<T>
    {
        Task Save(IEnumerable<T> weatherForecasts);

        Task<IReadOnlyDictionary<string, T>> Get(IEnumerable<string> ids);
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

        public async Task<IReadOnlyDictionary<string, WeatherForecast>> Get(IEnumerable<string> ids)
        {
            var enumerable = ids as string[] ?? ids.ToArray();
            var promises =
                new Dictionary<string, Task<(WeatherForecast WeatherForecast, string Id)>>(enumerable.Length);

            foreach (var id in enumerable)
            {
                var stateAndETagAsync = _daprClient.GetStateAndETagAsync<WeatherForecast>(nameof(WeatherForecast),
                    id);

                promises.Add(id, stateAndETagAsync);
            }

            var results = new Dictionary<string, WeatherForecast>(promises.Count);
            foreach (var promise in promises)
            {
                if (promise.Value.IsCompletedSuccessfully)
                {
                    results.Add(promise.Key, promise.Value.Result.WeatherForecast);
                    continue;
                }

                var slowTaskResult = await promise.Value;
                results.Add(promise.Key, slowTaskResult.WeatherForecast);
            }

            return results;
        }
    }
}