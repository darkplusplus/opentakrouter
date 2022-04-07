using SQLite;
using System;

namespace dpp.opentakrouter.Models
{
    public class StoredMessage
    {
        [PrimaryKey, AutoIncrement]
        public int PrimaryKey { get; set; }

        [Indexed]
        public string Uid { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Indexed]
        public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(5);

        [Ignore]
        public bool IsExpired { get { return DateTime.Now > Expiration; } }
    }
}
