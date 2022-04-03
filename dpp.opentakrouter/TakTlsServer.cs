using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakTlsServer : SslServer
    {
        public IRouter Router;

        public TakTlsServer(SslContext context, IPAddress address, int port, IRouter router) : base(context, address, port)
        {
            this.Router = router;
            this.Router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected override SslSession CreateSession()
        {
            return new TakTlsSession(this);
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            _ = this.Multicast(e.Data);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=tak-ssl error={error}");
        }
    }
}
