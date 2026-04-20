using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;

namespace dpp.opentakrouter
{
    class Program
    {
        static IHostBuilder Initialize(string[] args)
        {
            var configFile = ResolveConfigFile(args);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddJsonFile(configFile, optional: false)
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
                    var storage = context.Configuration.GetSection("server:storage").Get<StorageOptions>() ?? new StorageOptions();
                    services.AddDbContext<OpenTakRouterDbContext>(options =>
                    {
                        if (string.Equals(storage.Provider, "postgres", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(storage.Postgres?.ConnectionString))
                            {
                                throw new InvalidOperationException("server:storage:postgres:connectionString is required when using the postgres storage provider");
                            }

                            options.UseNpgsql(storage.Postgres.ConnectionString);
                        }
                        else
                        {
                            var sqlitePath = DatabaseInitializationService.ResolveSqlitePath(context.Configuration, storage);
                            options.UseSqlite($"Data Source={sqlitePath}");
                        }
                    });
                    services.AddScoped<IClientRepository, ClientRepository>();
                    services.AddScoped<IMessageRepository, MessageRepository>();
                    services.AddScoped<IDataPackageRepository, DataPackageRepository>();
                    services.AddSingleton<ProvisioningPackageService>();
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
                                var certificate = CertificateOptions.Load(apiConfig.Cert, apiConfig.Key, apiConfig.Passphrase);
                                serverOptions.Listen(IPAddress.Any, apiConfig.Port ?? 8443, listenOptions =>
                                {
                                    listenOptions.UseConnectionLogging();
                                    listenOptions.UseHttps(certificate);
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


            var host = Initialize(args).Build();
            await DatabaseInitializationService.InitializeAsync(
                host.Services,
                host.Services.GetRequiredService<IConfiguration>());
            await host.RunAsync();
        }

        private static string ResolveConfigFile(string[] args)
        {
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                if (string.Equals(arg, "--config", StringComparison.OrdinalIgnoreCase))
                {
                    if ((index + 1) >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                    {
                        throw new InvalidOperationException("--config requires a file path");
                    }

                    return args[index + 1];
                }

                const string prefix = "--config=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var path = arg[prefix.Length..];
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        throw new InvalidOperationException("--config requires a file path");
                    }

                    return path;
                }
            }

            return "opentakrouter.json";
        }
    }
}
