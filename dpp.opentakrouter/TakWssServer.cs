using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace dpp.opentakrouter
{
    public class TakWssServer : WssServer
    {
        public IRouter Router;

        public TakWssServer(SslContext context, IPAddress address, int port, IRouter router) : base(context, address, port)
        {
            this.Router = router;
            this.Router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected override WssSession CreateSession()
        {
            return new TakWssSession(this);
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            if (!Router.ShouldRouteTo(e.Envelope, "server:wss"))
            {
                return;
            }

            _ = this.MulticastText(UiEventMessage.Serialize(e.Envelope));
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=wss id=server error=true message=\"{error}\"");
        }
    }
}
