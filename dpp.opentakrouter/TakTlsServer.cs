using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakTlsServer : SslServer
    {
        public IRouter Router;
        public TakProtocolPreference ProtocolPreference { get; }

        public TakTlsServer(SslContext context, IPAddress address, int port, IRouter router, TakProtocolPreference protocolPreference) : base(context, address, port)
        {
            this.Router = router;
            ProtocolPreference = protocolPreference;
        }

        protected override SslSession CreateSession()
        {
            return new TakTlsSession(this);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-ssl error=true message=\"{error}\"");
        }
    }
}
