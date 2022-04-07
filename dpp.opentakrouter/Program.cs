using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace dpp.opentakrouter
{
    class Program
    {
        static IHostBuilder Initialize(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddJsonFile("opentakrouter.json", optional: true)
                .Build();

            var dataDir = Path.GetFullPath(
                configuration.GetValue("server:data",
                Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
                );

            var logFile = Path.Combine(dataDir, "opentakrouter.log");
            var flushInterval = new TimeSpan(0, 0, 1);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    logFile,
                    flushToDiskInterval: flushInterval,
                    rollingInterval: RollingInterval.Day)
                .CreateBootstrapLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(
                        logFile,
                        flushToDiskInterval: flushInterval,
                        rollingInterval: RollingInterval.Day))
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<IDatabaseContext, DatabaseContext>();
                    services.AddScoped<IClientRepository, ClientRepository>();
                    services.AddScoped<IMessageRepository, MessageRepository>();
                    services.AddScoped<IDataPackageRepository, DataPackageRepository>();
                    services.AddSingleton<IRouter, Router>();
                    services.AddHostedService<TakService>();
                })
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseContentRoot(dataDir);
                    builder.ConfigureKestrel((context, serverOptions) =>
                    {
                        var apiConfig = configuration.GetSection("server:api").Get<WebConfig>();
                        if (apiConfig is not null && apiConfig.Enabled)
                        {
                            if (apiConfig.Ssl)
                            {
                                serverOptions.Listen(IPAddress.Any, apiConfig.Port ?? 8443, listenOptions =>
                                {
                                    listenOptions.UseConnectionLogging();
                                    listenOptions.UseHttps(
                                        apiConfig.Cert,
                                        apiConfig.Passphrase
                                    );
                                });
                            }
                            else
                            {
                                serverOptions.Listen(IPAddress.Any, apiConfig.Port ?? 8080, listenOptions =>
                                {
                                    listenOptions.UseConnectionLogging();
                                });
                            }
                        }
                    });
                    builder.UseStartup<WebService>();
                })
                .UseWindowsService()
                .UseSystemd();

        }
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (Console.LargestWindowWidth != 0)
                {

                }
            }


            var hostBuilder = Initialize(args);

            await hostBuilder
                .Build()
                .RunAsync();
        }
    }
}
