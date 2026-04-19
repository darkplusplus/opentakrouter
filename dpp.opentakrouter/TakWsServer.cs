using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakWsServer : WsServer
    {
        public IRouter Router;

        public TakWsServer(IPAddress address, int port, IRouter router) : base(address, port)
        {
            this.Router = router;
            this.Router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected override WsSession CreateSession()
        {
            return new TakWsSession(this);
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            if (!Router.ShouldRouteTo(e.Envelope, "server:ws"))
            {
                return;
            }

            _ = this.MulticastText(UiEventMessage.Serialize(e.Envelope));
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=ws error=true message=\"{error}\"");
        }
    }
}
