using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public class WebConfig
    {
        public bool Enabled { get; set; } = true;
        public int? Port { get; set; }
        public bool Swagger { get; set; } = true;
        public bool Ssl { get; set; } = false;
        public string Cert { get; set; } = "";
        public string Passphrase { get; set; } = "";
    }
}
