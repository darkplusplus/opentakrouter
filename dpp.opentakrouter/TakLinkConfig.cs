namespace dpp.opentakrouter
{
    public class TakLinkConfig
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string Role { get; set; } = "listen";
        public string Transport { get; set; } = "tcp";
        public string Address { get; set; } = "";
        public int Port { get; set; }
        public string Cert { get; set; } = "";
        public string Passphrase { get; set; } = "";
        public string Mode { get; set; } = "duplex";
        public string Protocol { get; set; } = "xml";
    }
}
