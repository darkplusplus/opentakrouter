using SQLite;

namespace dpp.opentakrouter
{
    public interface IDatabaseContext
    {
        public SQLiteConnection Database { get; set; }
    }
}
