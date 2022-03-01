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
    public class TakServer : TcpServer
    {
        public Router Router;

        public TakServer(IPAddress address, int port) : base(address, port)
        {
            this.Router = new Router();
            this.Router.RaiseRoutedEvent += OnRoutedEvent;
        }

        protected override TcpSession CreateSession()
        {
            return new TakSession(this);
        }

        protected void OnRoutedEvent(object sender, RoutedEventArgs e)
        {
            if (e.Raw is null)
            {
                _ = this.Multicast(e.Event.ToXmlString());
            }
            else
            {
                _ = this.Multicast(e.Raw, 0, e.Raw.Length);
            }
        }

        protected override void OnError(SocketError error)
        {
            Log.Error($"id=server error={error}");
        }
    }
}
