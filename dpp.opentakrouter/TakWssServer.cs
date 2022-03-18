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
            var msg = new cot.Message() { Event = e.Event };
            _ = this.MulticastText(msg.ToXmlString());
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id=server error={error}");
        }
    }
}
