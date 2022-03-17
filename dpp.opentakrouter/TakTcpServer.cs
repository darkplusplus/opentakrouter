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
            var msg = new cot.Message() { Event = e.Event };
            _ = this.Multicast(msg.ToXmlString());
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id=server error={error}");
        }
    }
}
