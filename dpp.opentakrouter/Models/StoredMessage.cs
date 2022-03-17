using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dpp.opentakrouter.Models
{
    public class StoredMessage
    {
        [PrimaryKey, AutoIncrement]
        public int PrimaryKey { get; set; }

        public string Uid { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(5);

        [Ignore]
        public bool IsExpired { get { return DateTime.Now > Expiration; } }
    }
}
