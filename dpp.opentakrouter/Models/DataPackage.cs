using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dpp.opentakrouter.Models
{
    public class DataPackage
    {
        [PrimaryKey, AutoIncrement]
        public int PrimaryKey { get; set; }
        public string UID { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public DateTime SubmissionDateTime { get; set; } = DateTime.Now;
        public string SubmissionUser { get; set; }
        public string CreatorUid { get; set; }
        public string Keywords { get; set; }
        public string MIMEType { get; set; }
        public long Size { get; set; }
        public bool IsPrivate { get; set; }

        public byte[] Content { get; set; }
    }
}
