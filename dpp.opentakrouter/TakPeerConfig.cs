namespace dpp.opentakrouter
{
    public class TakPeerConfig
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public bool Ssl { get; set; } = false;
        public string Mode { get; set; } = "duplex";
    }
}
