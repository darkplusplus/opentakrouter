using System;

namespace dpp.opentakrouter.Models
{
    public class Client
    {
        public int PrimaryKey { get; set; }
        public string Uid { get; set; }
        public string Callsign { get; set; }
        public DateTime LastSeen { get; set; }
        public string LastStatus { get; set; } = "Connected";
        public string Device { get; set; }
        public string Platform { get; set; }
        public string Version { get; set; }
    }
}
