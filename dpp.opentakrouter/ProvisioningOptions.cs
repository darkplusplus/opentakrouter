namespace dpp.opentakrouter
{
    public class ProvisioningOptions
    {
        public string PackageName { get; set; } = "OpenTAKRouter";
        public string ServerDescription { get; set; } = "OpenTAKRouter";
        public string Callsign { get; set; } = "CALLSIGN";
        public string Team { get; set; } = "Blue";
        public string Role { get; set; } = "Team Member";
        public string TrustStoreCertificate { get; set; } = "";
        public string TrustStorePassword { get; set; } = "";
        public string ClientCertificate { get; set; } = "";
        public string ClientCertificatePassword { get; set; } = "";
        public string PublicApiScheme { get; set; } = "";
        public int PublicApiPort { get; set; }
        public bool OnReceiveDelete { get; set; } = true;
    }
}
