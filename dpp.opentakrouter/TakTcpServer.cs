using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakTcpServer : TcpServer
    {
        public IRouter Router;
        public TakProtocolPreference ProtocolPreference { get; }

        public TakTcpServer(IPAddress address, int port, IRouter router, TakProtocolPreference protocolPreference) : base(address, port)
        {
            this.Router = router;
            ProtocolPreference = protocolPreference;
        }

        protected override TcpSession CreateSession()
        {
            return new TakTcpSession(this);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-tcp error=true message=\"{error}\"");
        }
    }
}
