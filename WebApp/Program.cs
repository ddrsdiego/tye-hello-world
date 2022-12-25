namespace WebApp
{
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using Repositories;

    public static class Program
    {
        public static async Task Main(string[] args) => await CreateHostBuilder(args).Build().RunAsync();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDaprClient(builder =>
                    {
                        builder.UseHttpEndpoint("http://localhost:5010");
                        builder.UseGrpcEndpoint("http://localhost:50010");
                    });
                    services.AddControllers();
                    services.AddSingleton<IWeatherForecastRepository, WeatherForecastRepository>();
            
                    services.AddSwaggerGen(c =>
                        c.SwaggerDoc("v1", new OpenApiInfo {Title = Assembly.GetEntryAssembly()?.GetName().Name}));
                });
    }
}