using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakTcpServer : TcpServer
    {
        public IRouter Router;

        public TakTcpServer(IPAddress address, int port, IRouter router) : base(address, port)
        {
            this.Router = router;
            this.Router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected override TcpSession CreateSession()
        {
            return new TakTcpSession(this);
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            _ = this.Multicast(e.Data);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-tcp error={error}");
        }
    }
}
