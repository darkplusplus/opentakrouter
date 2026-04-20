namespace dpp.opentakrouter
{
    public class TakServerConfig
    {
        public bool Enabled { get; set; } = false;
        public int Port { get; set; }
        public string Cert { get; set; } = "";
        public string Key { get; set; } = "";
        public string Passphrase { get; set; } = "";
        public string Protocol { get; set; } = "xml";
    }
}
