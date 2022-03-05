using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using NetCoreServer;
using Serilog;

namespace dpp.opentakrouter
{
    public class TakTlsServer : SslServer
    {
        public Router Router;

        public TakTlsServer(SslContext context, IPAddress address, int port, Router router) : base(context, address, port)
        {
            this.Router = router ?? new Router();
            this.Router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected override SslSession CreateSession()
        {
            return new TakTlsSession(this);
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {

            var msg = new cot.Message() { Event = e.Event };
            _ = this.Multicast(msg.ToXmlString());
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id=server error={error}");
        }
    }
}
