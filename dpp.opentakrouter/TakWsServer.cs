using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
            // TODO: can websockets be raw data (i.e. protobuf), or should they just be the xml event?
            var xml = e.Event.ToXmlString();
            _ = this.MulticastText(xml);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"server=ws error={error}");
        }
    }
}
