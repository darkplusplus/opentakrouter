using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace dpp.opentakrouter
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddJsonFile("opentakrouter.json")//, true)
                .Build();
                
            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<TakService>();
                })
                .Build()
                .RunAsync();
        }                                                       
    }
}
