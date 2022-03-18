using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace dpp.opentakrouter
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddJsonFile("opentakrouter.json", true)
                .Build();

            var dataDir = Path.GetFullPath(configuration.GetValue("server:data", Path.GetDirectoryName(
                Process.GetCurrentProcess().MainModule.FileName)));
            
            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<IDatabaseContext, DatabaseContext>();
                    services.AddScoped<IClientRepository, ClientRepository>();
                    services.AddScoped<IMessageRepository, MessageRepository>();
                    services.AddScoped<IDataPackageRepository, DataPackageRepository>();
                    services.AddScoped<IRouter, Router>();
                    services.AddHostedService<TakService>();
                })
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseContentRoot(dataDir);
                    builder.ConfigureKestrel((context, serverOptions) =>
                    {
                        if (configuration.GetValue("server:web:ssl", false))
                        {
                            serverOptions.Listen(IPAddress.Any, configuration.GetValue("server:web:port", 8443), listenOptions =>
                            {
                                listenOptions.UseConnectionLogging();
                                listenOptions.UseHttps(
                                    configuration.GetValue<string>("server:web:cert"),
                                    configuration.GetValue<string>("server:web:passphrase")
                                );
                            });
                        }
                        else
                        {
                            serverOptions.Listen(IPAddress.Any, configuration.GetValue("server:web:port", 8080), listenOptions =>
                            {
                                listenOptions.UseConnectionLogging();
                            });
                        }
                    });
                    builder.UseStartup<WebService>();
                })
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console())
                .Build()
                .RunAsync();
        }
    }
}
