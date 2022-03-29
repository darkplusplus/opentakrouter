using NetCoreServer;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
            var xml = Encoding.UTF8.GetString(e.Data);
            _ = this.MulticastText(xml);
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id=server error={error}");
        }
    }
}
