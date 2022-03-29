using SQLite;
using System;

namespace dpp.opentakrouter.Models
{
    public class Client
    {
        [PrimaryKey, AutoIncrement]
        public int PrimaryKey { get; set; }
        public string Callsign { get; set; }
        public DateTime LastSeen { get; set; }
        public string Device { get; set; }
        public string Platform { get; set; }
        public string Version { get; set; }
    }
}
