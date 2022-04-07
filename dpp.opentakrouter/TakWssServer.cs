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
            // TODO: can websockets be raw data (i.e. protobuf), or should they just be the xml event?
            var xml = e.Event.ToXmlString();
            _ = this.MulticastText(xml);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=wss id=server error={error}");
        }
    }
}
