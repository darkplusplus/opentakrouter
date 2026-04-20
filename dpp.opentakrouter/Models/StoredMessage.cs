using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace dpp.opentakrouter.Models
{
    public class StoredMessage
    {
        public int PrimaryKey { get; set; }
        public string Uid { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(5);

        [NotMapped]
        public bool IsExpired { get { return DateTime.Now > Expiration; } }
    }
}
