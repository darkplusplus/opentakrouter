using Microsoft.Extensions.Configuration;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public class DatabaseContext : IDatabaseContext
    {
        public SQLiteConnection Database { get; set; }
        private readonly IConfiguration _configuration;

        public DatabaseContext(IConfiguration configuration)
        {
            _configuration = configuration;

            var dataDir = Environment.ExpandEnvironmentVariables(
                _configuration.GetValue("server:data", System.AppContext.BaseDirectory)
            );
            var dbPath = Path.Combine(dataDir, "opentakrouter.db");
            var options = new SQLiteConnectionString(
                dbPath,
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex,
                true,
                null, null, null, null);

            Database = new SQLiteConnection(options);
        }
    }
}
