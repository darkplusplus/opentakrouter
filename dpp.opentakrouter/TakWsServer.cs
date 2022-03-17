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
            var msg = new cot.Message() { Event = e.Event };
            _ = this.MulticastText(msg.ToXmlString());
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id=server error={error}");
        }
    }
}
