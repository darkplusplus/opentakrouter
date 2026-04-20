using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public static class DatabaseInitializationService
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenTakRouterDbContext>();

            await db.Database.EnsureCreatedAsync(cancellationToken);

            var storage = configuration.GetSection("server:storage").Get<StorageOptions>() ?? new StorageOptions();
            if (string.Equals(storage.Provider, "sqlite", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
                await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken);
                Log.Information("storage=sqlite state=ready wal=true");
            }
            else
            {
                Log.Information("storage=postgres state=ready");
            }
        }

        public static string ResolveSqlitePath(IConfiguration configuration, StorageOptions storage)
        {
            var dataDir = Environment.ExpandEnvironmentVariables(
                configuration.GetValue("server:data", AppContext.BaseDirectory));
            Directory.CreateDirectory(dataDir);

            var configuredPath = storage.Sqlite?.Path;
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = "opentakrouter.db";
            }

            if (Path.IsPathRooted(configuredPath))
            {
                var rootedDir = Path.GetDirectoryName(configuredPath);
                if (!string.IsNullOrWhiteSpace(rootedDir))
                {
                    Directory.CreateDirectory(rootedDir);
                }

                return configuredPath;
            }

            return Path.Combine(dataDir, configuredPath);
        }
    }
}
