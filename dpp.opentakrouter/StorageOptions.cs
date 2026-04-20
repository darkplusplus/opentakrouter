namespace dpp.opentakrouter
{
    public class StorageOptions
    {
        public string Provider { get; set; } = "sqlite";
        public SqliteStorageOptions Sqlite { get; set; } = new();
        public PostgresStorageOptions Postgres { get; set; } = new();
    }

    public class SqliteStorageOptions
    {
        public string Path { get; set; } = "opentakrouter.db";
    }

    public class PostgresStorageOptions
    {
        public string ConnectionString { get; set; } = "";
    }
}
